using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using erp.Data;
using erp.DTOs.Reports;
using erp.Models.Financial;
using erp.Models.Sales;
using erp.Models.ServiceOrders;
using erp.Services.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace erp.Tests.Services.Reports;

public class FinancialReportAccuracyTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GenerateCashFlowReport_AppliesCategoryAndPaymentMethodFilters()
    {
        await using var context = CreateContext();
        var customer = new Customer { Id = 1, TenantId = 1, Name = "Cliente A", Document = "12345678901" };
        var categorySales = new FinancialCategory { Id = 10, Name = "Vendas", Type = CategoryType.Revenue, Code = "REC", IsActive = true, TenantId = 1 };
        var categoryServices = new FinancialCategory { Id = 11, Name = "Servicos", Type = CategoryType.Revenue, Code = "SER", IsActive = true, TenantId = 1 };

        context.Customers.Add(customer);
        context.FinancialCategories.AddRange(categorySales, categoryServices);
        context.AccountsReceivable.AddRange(
            new AccountReceivable
            {
                Id = 1,
                TenantId = 1,
                CustomerId = 1,
                OriginalAmount = 100m,
                DueDate = DateTime.UtcNow,
                IssueDate = DateTime.UtcNow,
                Status = AccountStatus.Paid,
                PaymentMethod = PaymentMethod.Pix,
                CategoryId = 10,
                CreatedByUserId = 1
            },
            new AccountReceivable
            {
                Id = 2,
                TenantId = 1,
                CustomerId = 1,
                OriginalAmount = 80m,
                DueDate = DateTime.UtcNow,
                IssueDate = DateTime.UtcNow,
                Status = AccountStatus.Paid,
                PaymentMethod = PaymentMethod.Cash,
                CategoryId = 11,
                CreatedByUserId = 1
            });

        await context.SaveChangesAsync();

        var service = new FinancialReportService(context, NullLogger<FinancialReportService>.Instance);

        var result = await service.GenerateCashFlowReportAsync(new FinancialReportFilterDto
        {
            CategoryId = 10,
            PaymentMethod = PaymentMethod.Pix
        });

        result.Items.Should().HaveCount(1);
        result.Items[0].Category.Should().Be("Vendas");
        result.Items[0].PaymentMethod.Should().Be("Pix");
        result.Summary.TotalRevenue.Should().Be(100m);
        result.Summary.RevenueByPaymentMethod.Should().ContainKey("Pix");
        result.Summary.RevenueByPaymentMethod["Pix"].Should().Be(100m);
    }

    [Fact]
    public async Task GenerateProfitLossReport_UsesCompetenceRevenueFromFinalizedSalesAndDeliveredOrders()
    {
        await using var context = CreateContext();

        context.Sales.AddRange(
            new Sale
            {
                Id = 1,
                TenantId = 1,
                SaleNumber = "VEN1",
                UserId = 1,
                SaleDate = DateTime.UtcNow.Date,
                Status = "Finalizada",
                TotalAmount = 120m,
                NetAmount = 110m,
                DiscountAmount = 10m
            },
            new Sale
            {
                Id = 2,
                TenantId = 1,
                SaleNumber = "VEN2",
                UserId = 1,
                SaleDate = DateTime.UtcNow.Date,
                Status = "Pendente",
                TotalAmount = 200m,
                NetAmount = 200m
            });

        context.ServiceOrders.AddRange(
            new ServiceOrder
            {
                Id = 1,
                TenantId = 1,
                OrderNumber = "OS1",
                UserId = 1,
                EntryDate = DateTime.UtcNow.Date,
                Status = ServiceOrderStatus.Delivered.ToString(),
                TotalAmount = 90m,
                NetAmount = 90m
            },
            new ServiceOrder
            {
                Id = 2,
                TenantId = 1,
                OrderNumber = "OS2",
                UserId = 1,
                EntryDate = DateTime.UtcNow.Date,
                Status = ServiceOrderStatus.Cancelled.ToString(),
                TotalAmount = 999m,
                NetAmount = 999m
            });

        await context.SaveChangesAsync();

        var service = new FinancialReportService(context, NullLogger<FinancialReportService>.Instance);
        var result = await service.GenerateProfitLossReportAsync(new FinancialReportFilterDto());

        result.TotalRevenue.Should().Be(200m);
    }

    [Fact]
    public async Task GenerateDailyClosingReport_ComputesOpeningAndClosingBalancesFromRealizedMovements()
    {
        await using var context = CreateContext();
        var customer = new Customer { Id = 1, TenantId = 1, Name = "Cliente Fechamento", Document = "12345678901" };
        var supplier = new Supplier { Id = 1, TenantId = 1, Name = "Fornecedor Fechamento", TaxId = "10987654321" };

        context.Customers.Add(customer);
        context.Suppliers.Add(supplier);

        var reportDate = new DateTime(2026, 2, 5);

        context.AccountsReceivable.AddRange(
            new AccountReceivable
            {
                Id = 10,
                TenantId = 1,
                CustomerId = 1,
                OriginalAmount = 300m,
                PaidAmount = 300m,
                DueDate = reportDate.AddDays(-1),
                IssueDate = reportDate.AddDays(-5),
                PaymentDate = reportDate.AddDays(-1),
                Status = AccountStatus.Paid,
                PaymentMethod = PaymentMethod.Cash,
                CreatedByUserId = 1
            },
            new AccountReceivable
            {
                Id = 11,
                TenantId = 1,
                CustomerId = 1,
                OriginalAmount = 200m,
                PaidAmount = 200m,
                DueDate = reportDate,
                IssueDate = reportDate,
                PaymentDate = reportDate,
                Status = AccountStatus.Paid,
                PaymentMethod = PaymentMethod.Pix,
                CreatedByUserId = 1
            });

        context.AccountsPayable.AddRange(
            new AccountPayable
            {
                Id = 20,
                TenantId = 1,
                SupplierId = 1,
                OriginalAmount = 100m,
                PaidAmount = 100m,
                DueDate = reportDate.AddDays(-2),
                IssueDate = reportDate.AddDays(-6),
                PaymentDate = reportDate.AddDays(-2),
                Status = AccountStatus.Paid,
                PaymentMethod = PaymentMethod.BankTransfer,
                CreatedByUserId = 1
            },
            new AccountPayable
            {
                Id = 21,
                TenantId = 1,
                SupplierId = 1,
                OriginalAmount = 80m,
                PaidAmount = 80m,
                DueDate = reportDate,
                IssueDate = reportDate,
                PaymentDate = reportDate,
                Status = AccountStatus.Paid,
                PaymentMethod = PaymentMethod.Cash,
                CreatedByUserId = 1
            });

        await context.SaveChangesAsync();

        var service = new FinancialReportService(context, NullLogger<FinancialReportService>.Instance);
        var result = await service.GenerateDailyClosingReportAsync(new FinancialReportFilterDto { StartDate = reportDate });

        result.Summary.ReportDate.Date.Should().Be(reportDate.Date);
        result.Summary.OpeningBalance.Should().Be(200m);
        result.Summary.TotalEntriesRealized.Should().Be(200m);
        result.Summary.TotalExitsRealized.Should().Be(80m);
        result.Summary.NetRealized.Should().Be(120m);
        result.Summary.ClosingBalance.Should().Be(320m);
    }

    [Fact]
    public async Task GenerateDailyClosingReport_SeparatesDueMovementsAndOverdues()
    {
        await using var context = CreateContext();
        var customer = new Customer { Id = 2, TenantId = 1, Name = "Cliente B", Document = "12312312312" };
        var supplier = new Supplier { Id = 2, TenantId = 1, Name = "Fornecedor B", TaxId = "45645645645" };

        context.Customers.Add(customer);
        context.Suppliers.Add(supplier);

        var reportDate = new DateTime(2026, 2, 6);

        context.AccountsReceivable.AddRange(
            new AccountReceivable
            {
                Id = 30,
                TenantId = 1,
                CustomerId = 2,
                OriginalAmount = 150m,
                DueDate = reportDate,
                IssueDate = reportDate,
                Status = AccountStatus.Pending,
                PaymentMethod = PaymentMethod.CreditCard,
                CreatedByUserId = 1
            },
            new AccountReceivable
            {
                Id = 31,
                TenantId = 1,
                CustomerId = 2,
                OriginalAmount = 90m,
                DueDate = reportDate.AddDays(-3),
                IssueDate = reportDate.AddDays(-10),
                Status = AccountStatus.Overdue,
                PaymentMethod = PaymentMethod.Cash,
                CreatedByUserId = 1
            });

        context.AccountsPayable.AddRange(
            new AccountPayable
            {
                Id = 40,
                TenantId = 1,
                SupplierId = 2,
                OriginalAmount = 70m,
                DueDate = reportDate,
                IssueDate = reportDate,
                Status = AccountStatus.Pending,
                PaymentMethod = PaymentMethod.BankSlip,
                CreatedByUserId = 1
            },
            new AccountPayable
            {
                Id = 41,
                TenantId = 1,
                SupplierId = 2,
                OriginalAmount = 60m,
                DueDate = reportDate.AddDays(-1),
                IssueDate = reportDate.AddDays(-12),
                Status = AccountStatus.Overdue,
                PaymentMethod = PaymentMethod.Pix,
                CreatedByUserId = 1
            });

        await context.SaveChangesAsync();

        var service = new FinancialReportService(context, NullLogger<FinancialReportService>.Instance);
        var result = await service.GenerateDailyClosingReportAsync(new FinancialReportFilterDto { StartDate = reportDate });

        result.DueEntries.Should().HaveCount(1);
        result.DueExits.Should().HaveCount(1);
        result.Summary.TotalEntriesDue.Should().Be(150m);
        result.Summary.TotalExitsDue.Should().Be(70m);
        result.Summary.NetDue.Should().Be(80m);
        result.Summary.OverdueReceivablesCount.Should().Be(1);
        result.Summary.OverduePayablesCount.Should().Be(1);
        result.Summary.OverdueReceivablesAmount.Should().Be(90m);
        result.Summary.OverduePayablesAmount.Should().Be(60m);
    }
}
