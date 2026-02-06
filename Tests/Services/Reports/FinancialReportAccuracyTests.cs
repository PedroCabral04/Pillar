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
}
