using erp.Data;
using erp.DTOs.Reports;
using erp.Models.Financial;
using erp.Models.Inventory;
using erp.Models.Sales;
using erp.Services.Reports;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using erp.Services.Tenancy;
using Xunit;

namespace erp.Tests.Services.Reports;

public class FinancialReportServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var tenantAccessor = new Mock<ITenantContextAccessor>();
        tenantAccessor.SetupGet(x => x.Current).Returns(new TenantContext());

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options, tenantContextAccessor: tenantAccessor.Object);
    }

    [Fact]
    public async Task GenerateCashFlowReport_ComputesRevenueAndExpensesSummary()
    {
        await using var context = CreateContext();

        context.Customers.Add(new Customer { Id = 1, TenantId = 1, Name = "Cliente", Document = "12345678901" });
        context.Suppliers.Add(new Supplier { Id = 1, TenantId = 1, Name = "Fornecedor", TaxId = "12345678000199" });

        context.AccountsReceivable.Add(new AccountReceivable
        {
            Id = 1,
            TenantId = 1,
            CustomerId = 1,
            OriginalAmount = 100m,
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow,
            Status = AccountStatus.Paid,
            PaymentMethod = PaymentMethod.Pix,
            CreatedByUserId = 1
        });

        context.AccountsPayable.Add(new AccountPayable
        {
            Id = 1,
            TenantId = 1,
            SupplierId = 1,
            OriginalAmount = 40m,
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow,
            Status = AccountStatus.Paid,
            PaymentMethod = PaymentMethod.Pix,
            CreatedByUserId = 1
        });

        await context.SaveChangesAsync();

        var service = new FinancialReportService(context, NullLogger<FinancialReportService>.Instance);
        var result = await service.GenerateCashFlowReportAsync(new FinancialReportFilterDto());

        result.Summary.TotalRevenue.Should().Be(100m);
        result.Summary.TotalExpenses.Should().Be(40m);
        result.Summary.NetCashFlow.Should().Be(60m);
    }

    [Fact]
    public async Task GenerateBalanceSheetReport_UsesPendingBalancesAndInventory()
    {
        await using var context = CreateContext();

        context.AccountsReceivable.Add(new AccountReceivable
        {
            Id = 10,
            TenantId = 1,
            CustomerId = 1,
            OriginalAmount = 200m,
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow,
            Status = AccountStatus.Pending,
            CreatedByUserId = 1
        });

        context.AccountsPayable.Add(new AccountPayable
        {
            Id = 20,
            TenantId = 1,
            SupplierId = 1,
            OriginalAmount = 50m,
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow,
            Status = AccountStatus.Pending,
            CreatedByUserId = 1
        });

        context.ProductCategories.Add(new ProductCategory { Id = 1, Name = "Cat", Code = "CAT" });
        context.Products.Add(new Product
        {
            Id = 30,
            TenantId = 1,
            Name = "Produto",
            Sku = "SKU-1",
            CategoryId = 1,
            CurrentStock = 3,
            CostPrice = 20m,
            IsActive = true,
            CreatedByUserId = 1
        });

        await context.SaveChangesAsync();

        var service = new FinancialReportService(context, NullLogger<FinancialReportService>.Instance);
        var result = await service.GenerateBalanceSheetReportAsync(new FinancialReportFilterDto());

        result.CurrentAssets.Should().Be(200m);
        result.FixedAssets.Should().Be(60m);
        result.TotalLiabilities.Should().Be(50m);
        result.Equity.Should().Be(210m);
    }
}
