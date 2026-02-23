using erp.Data;
using erp.DTOs.Reports;
using erp.Models.Inventory;
using erp.Models.Identity;
using erp.Services.Reports;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using erp.Services.Tenancy;
using Xunit;

namespace erp.Tests.Services.Reports;

public class InventoryReportServiceTests
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
    public async Task GenerateStockLevelsReport_ComputesSummaryBuckets()
    {
        await using var context = CreateContext();

        context.ProductCategories.Add(new ProductCategory { Id = 1, Name = "Geral", Code = "GER" });
        context.Products.AddRange(
            new Product { Id = 1, TenantId = 1, Name = "P1", Sku = "P1", CategoryId = 1, CurrentStock = 10, MinimumStock = 3, CostPrice = 5, SalePrice = 9, CreatedByUserId = 1 },
            new Product { Id = 2, TenantId = 1, Name = "P2", Sku = "P2", CategoryId = 1, CurrentStock = 2, MinimumStock = 3, CostPrice = 5, SalePrice = 9, CreatedByUserId = 1 },
            new Product { Id = 3, TenantId = 1, Name = "P3", Sku = "P3", CategoryId = 1, CurrentStock = 0, MinimumStock = 3, CostPrice = 5, SalePrice = 9, CreatedByUserId = 1 });

        await context.SaveChangesAsync();

        var service = new InventoryReportService(context, NullLogger<InventoryReportService>.Instance);
        var result = await service.GenerateStockLevelsReportAsync(new InventoryReportFilterDto());

        result.Summary.TotalProducts.Should().Be(3);
        result.Summary.ProductsInStock.Should().Be(1);
        result.Summary.ProductsLowStock.Should().Be(1);
        result.Summary.ProductsOutOfStock.Should().Be(1);
    }

    [Fact]
    public async Task GenerateStockMovementReport_ReturnsMovementSummary()
    {
        await using var context = CreateContext();

        context.ProductCategories.Add(new ProductCategory { Id = 10, Name = "Mov", Code = "MOV" });
        context.Users.Add(new ApplicationUser { Id = 5, UserName = "usr", Email = "usr@erp.local" });
        context.Products.Add(new Product
        {
            Id = 10,
            TenantId = 1,
            Name = "Produto",
            Sku = "SKU-MOV",
            CategoryId = 10,
            CurrentStock = 20,
            CostPrice = 15,
            SalePrice = 30,
            CreatedByUserId = 5
        });

        context.StockMovements.AddRange(
            new StockMovement
            {
                Id = 1,
                TenantId = 1,
                ProductId = 10,
                Type = MovementType.In,
                Reason = MovementReason.Purchase,
                Quantity = 5,
                UnitCost = 10,
                CreatedByUserId = 5,
                MovementDate = DateTime.UtcNow.AddDays(-1)
            },
            new StockMovement
            {
                Id = 2,
                TenantId = 1,
                ProductId = 10,
                Type = MovementType.Out,
                Reason = MovementReason.Sale,
                Quantity = 2,
                UnitCost = 10,
                CreatedByUserId = 5,
                MovementDate = DateTime.UtcNow
            });

        await context.SaveChangesAsync();

        var service = new InventoryReportService(context, NullLogger<InventoryReportService>.Instance);
        var result = await service.GenerateStockMovementReportAsync(new InventoryReportFilterDto());

        result.Summary.TotalMovements.Should().Be(2);
        result.Summary.MovementsByType.Should().ContainKey("In");
        result.Summary.MovementsByType.Should().ContainKey("Out");
    }
}
