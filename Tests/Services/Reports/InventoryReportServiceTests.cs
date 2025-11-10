using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using erp.Data;
using erp.DTOs.Reports;
using erp.Models.Inventory;
using erp.Models.Sales;
using erp.Services.Reports;

namespace erp.Tests.Services.Reports;

/// <summary>
/// Testes unitários para o serviço de relatórios de estoque
/// </summary>
public class InventoryReportServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<InventoryReportService>> _mockLogger;
    private readonly InventoryReportService _service;

    public InventoryReportServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<InventoryReportService>>();
        _service = new InventoryReportService(_context, _mockLogger.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var warehouse1 = new Warehouse
        {
            Id = 1,
            Name = "Armazém Principal",
            Code = "ARM-001",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var warehouse2 = new Warehouse
        {
            Id = 2,
            Name = "Armazém Secundário",
            Code = "ARM-002",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var category1 = new Category
        {
            Id = 1,
            Name = "Eletrônicos",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var category2 = new Category
        {
            Id = 2,
            Name = "Vestuário",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var product1 = new Product
        {
            Id = 1,
            Name = "Notebook Dell",
            Sku = "NB-001",
            CategoryId = 1,
            CostPrice = 2000,
            SalePrice = 3000,
            MinimumStock = 5,
            MaximumStock = 50,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var product2 = new Product
        {
            Id = 2,
            Name = "Mouse Logitech",
            Sku = "MS-001",
            CategoryId = 1,
            CostPrice = 50,
            SalePrice = 100,
            MinimumStock = 20,
            MaximumStock = 100,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var product3 = new Product
        {
            Id = 3,
            Name = "Camiseta Básica",
            Sku = "CAM-001",
            CategoryId = 2,
            CostPrice = 20,
            SalePrice = 50,
            MinimumStock = 50,
            MaximumStock = 200,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Stock in warehouses
        var stock1 = new Stock
        {
            Id = 1,
            ProductId = 1,
            WarehouseId = 1,
            Quantity = 10,
            UpdatedAt = DateTime.UtcNow
        };

        var stock2 = new Stock
        {
            Id = 2,
            ProductId = 2,
            WarehouseId = 1,
            Quantity = 15, // Below minimum
            UpdatedAt = DateTime.UtcNow
        };

        var stock3 = new Stock
        {
            Id = 3,
            ProductId = 3,
            WarehouseId = 2,
            Quantity = 3, // Critical - well below minimum
            UpdatedAt = DateTime.UtcNow
        };

        // Stock movements
        var movement1 = new StockMovement
        {
            Id = 1,
            ProductId = 1,
            WarehouseId = 1,
            Type = "Entrada",
            Quantity = 20,
            Reason = "Compra de fornecedor",
            UserId = 1,
            MovementDate = DateTime.UtcNow.AddDays(-10),
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        var movement2 = new StockMovement
        {
            Id = 2,
            ProductId = 1,
            WarehouseId = 1,
            Type = "Saída",
            Quantity = 10,
            Reason = "Venda",
            UserId = 1,
            MovementDate = DateTime.UtcNow.AddDays(-5),
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        var movement3 = new StockMovement
        {
            Id = 3,
            ProductId = 2,
            WarehouseId = 1,
            Type = "Entrada",
            Quantity = 30,
            Reason = "Compra de fornecedor",
            UserId = 1,
            MovementDate = DateTime.UtcNow.AddDays(-8),
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        };

        var movement4 = new StockMovement
        {
            Id = 4,
            ProductId = 2,
            WarehouseId = 1,
            Type = "Saída",
            Quantity = 15,
            Reason = "Venda",
            UserId = 1,
            MovementDate = DateTime.UtcNow.AddDays(-3),
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };

        var movement5 = new StockMovement
        {
            Id = 5,
            ProductId = 3,
            WarehouseId = 2,
            Type = "Ajuste",
            Quantity = -5,
            Reason = "Perda/Avaria",
            UserId = 1,
            MovementDate = DateTime.UtcNow.AddDays(-2),
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        _context.Warehouses.AddRange(warehouse1, warehouse2);
        _context.Categories.AddRange(category1, category2);
        _context.Products.AddRange(product1, product2, product3);
        _context.Stocks.AddRange(stock1, stock2, stock3);
        _context.StockMovements.AddRange(movement1, movement2, movement3, movement4, movement5);
        _context.SaveChanges();
    }

    #region Stock Levels Tests

    [Fact]
    public async Task GenerateStockLevelsReport_ReturnsAllProducts()
    {
        // Arrange
        var filter = new InventoryReportFilterDto();

        // Act
        var result = await _service.GenerateStockLevelsReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalProducts.Should().Be(3);
        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GenerateStockLevelsReport_IdentifiesLowStock()
    {
        // Arrange
        var filter = new InventoryReportFilterDto();

        // Act
        var result = await _service.GenerateStockLevelsReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.LowStockItems.Should().Be(1); // Product 2 (Mouse)
    }

    [Fact]
    public async Task GenerateStockLevelsReport_IdentifiesCriticalStock()
    {
        // Arrange
        var filter = new InventoryReportFilterDto();

        // Act
        var result = await _service.GenerateStockLevelsReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.CriticalStockItems.Should().Be(1); // Product 3 (Camiseta)
    }

    [Fact]
    public async Task GenerateStockLevelsReport_WithLowStockFilter_ReturnsOnlyLowStock()
    {
        // Arrange
        var filter = new InventoryReportFilterDto
        {
            OnlyLowStock = true
        };

        // Act
        var result = await _service.GenerateStockLevelsReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2); // Product 2 and 3
        result.Items.Should().OnlyContain(i => 
            i.Status == "Baixo" || i.Status == "Crítico");
    }

    [Fact]
    public async Task GenerateStockLevelsReport_WithCategoryFilter_ReturnsFilteredProducts()
    {
        // Arrange
        var filter = new InventoryReportFilterDto
        {
            CategoryId = 1 // Eletrônicos
        };

        // Act
        var result = await _service.GenerateStockLevelsReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2); // Notebook and Mouse
        result.Items.Should().OnlyContain(i => i.CategoryName == "Eletrônicos");
    }

    [Fact]
    public async Task GenerateStockLevelsReport_WithWarehouseFilter_ReturnsFilteredStock()
    {
        // Arrange
        var filter = new InventoryReportFilterDto
        {
            WarehouseId = 1
        };

        // Act
        var result = await _service.GenerateStockLevelsReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2); // Products in warehouse 1
        result.Items.Should().OnlyContain(i => i.WarehouseName == "Armazém Principal");
    }

    [Fact]
    public async Task GenerateStockLevelsReport_CalculatesStockValue()
    {
        // Arrange
        var filter = new InventoryReportFilterDto();

        // Act
        var result = await _service.GenerateStockLevelsReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalStockValue.Should().BeGreaterThan(0);
        // Notebook: 10 * 2000 = 20000
        // Mouse: 15 * 50 = 750
        // Camiseta: 3 * 20 = 60
        // Total should be around 20810
        result.TotalStockValue.Should().BeApproximately(20810, 1);
    }

    [Fact]
    public async Task GenerateStockLevelsReport_WithSearchTerm_FiltersProducts()
    {
        // Arrange
        var filter = new InventoryReportFilterDto
        {
            SearchTerm = "Dell"
        };

        // Act
        var result = await _service.GenerateStockLevelsReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().ProductName.Should().Contain("Dell");
    }

    #endregion

    #region Stock Movement Tests

    [Fact]
    public async Task GenerateStockMovementReport_ReturnsAllMovements()
    {
        // Arrange
        var filter = new InventoryReportFilterDto();

        // Act
        var result = await _service.GenerateStockMovementReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalMovements.Should().Be(5);
        result.Movements.Should().HaveCount(5);
    }

    [Fact]
    public async Task GenerateStockMovementReport_WithDateRange_ReturnsFilteredMovements()
    {
        // Arrange
        var filter = new InventoryReportFilterDto
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow
        };

        // Act
        var result = await _service.GenerateStockMovementReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.Movements.Should().NotBeEmpty();
        result.Movements.Should().OnlyContain(m => 
            m.MovementDate >= filter.StartDate && 
            m.MovementDate <= filter.EndDate);
    }

    [Fact]
    public async Task GenerateStockMovementReport_WithMovementTypeFilter_ReturnsFilteredMovements()
    {
        // Arrange
        var filter = new InventoryReportFilterDto
        {
            MovementType = "Entrada"
        };

        // Act
        var result = await _service.GenerateStockMovementReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.Movements.Should().HaveCount(2); // 2 entry movements
        result.Movements.Should().OnlyContain(m => m.Type == "Entrada");
    }

    [Fact]
    public async Task GenerateStockMovementReport_CalculatesTotalValue()
    {
        // Arrange
        var filter = new InventoryReportFilterDto();

        // Act
        var result = await _service.GenerateStockMovementReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalValueMovements.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateStockMovementReport_GroupsByType()
    {
        // Arrange
        var filter = new InventoryReportFilterDto();

        // Act
        var result = await _service.GenerateStockMovementReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        var types = result.Movements.Select(m => m.Type).Distinct().ToList();
        types.Should().Contain("Entrada");
        types.Should().Contain("Saída");
        types.Should().Contain("Ajuste");
    }

    [Fact]
    public async Task GenerateStockMovementReport_OrdersByDateDescending()
    {
        // Arrange
        var filter = new InventoryReportFilterDto();

        // Act
        var result = await _service.GenerateStockMovementReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        var dates = result.Movements.Select(m => m.MovementDate).ToList();
        dates.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GenerateStockMovementReport_WithProductFilter_ReturnsProductMovements()
    {
        // Arrange
        var filter = new InventoryReportFilterDto
        {
            ProductId = 1 // Notebook
        };

        // Act
        var result = await _service.GenerateStockMovementReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.Movements.Should().HaveCount(2); // 1 entry + 1 exit
        result.Movements.Should().OnlyContain(m => m.ProductId == 1);
    }

    #endregion

    #region Inventory Valuation Tests

    [Fact]
    public async Task GenerateInventoryValuationReport_GroupsByCategory()
    {
        // Arrange
        var filter = new InventoryReportFilterDto();

        // Act
        var result = await _service.GenerateInventoryValuationReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.Categories.Should().HaveCount(2); // Eletrônicos and Vestuário
    }

    [Fact]
    public async Task GenerateInventoryValuationReport_CalculatesCategoryValues()
    {
        // Arrange
        var filter = new InventoryReportFilterDto();

        // Act
        var result = await _service.GenerateInventoryValuationReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        
        var electronics = result.Categories.FirstOrDefault(c => c.CategoryName == "Eletrônicos");
        electronics.Should().NotBeNull();
        electronics!.ProductCount.Should().Be(2);
        electronics.TotalQuantity.Should().Be(25); // 10 + 15
        
        var clothing = result.Categories.FirstOrDefault(c => c.CategoryName == "Vestuário");
        clothing.Should().NotBeNull();
        clothing!.ProductCount.Should().Be(1);
        clothing.TotalQuantity.Should().Be(3);
    }

    [Fact]
    public async Task GenerateInventoryValuationReport_CalculatesProfitMargin()
    {
        // Arrange
        var filter = new InventoryReportFilterDto();

        // Act
        var result = await _service.GenerateInventoryValuationReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.Categories.Should().OnlyContain(c => c.ProfitMargin >= 0);
        
        var electronics = result.Categories.FirstOrDefault(c => c.CategoryName == "Eletrônicos");
        electronics.Should().NotBeNull();
        electronics!.PotentialProfit.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateInventoryValuationReport_CalculatesTotalValue()
    {
        // Arrange
        var filter = new InventoryReportFilterDto();

        // Act
        var result = await _service.GenerateInventoryValuationReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalCostValue.Should().BeGreaterThan(0);
        result.TotalSaleValue.Should().BeGreaterThan(result.TotalCostValue);
        result.TotalPotentialProfit.Should().Be(result.TotalSaleValue - result.TotalCostValue);
    }

    [Fact]
    public async Task GenerateInventoryValuationReport_WithCategoryFilter_ReturnsOnlyCategory()
    {
        // Arrange
        var filter = new InventoryReportFilterDto
        {
            CategoryId = 1
        };

        // Act
        var result = await _service.GenerateInventoryValuationReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.Categories.Should().HaveCount(1);
        result.Categories.First().CategoryName.Should().Be("Eletrônicos");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GenerateStockLevelsReport_WithNoStock_ReturnsEmptyReport()
    {
        // Arrange
        _context.Stocks.RemoveRange(_context.Stocks);
        await _context.SaveChangesAsync();

        var filter = new InventoryReportFilterDto();

        // Act
        var result = await _service.GenerateStockLevelsReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalProducts.Should().Be(0);
        result.Items.Should().BeEmpty();
        result.TotalStockValue.Should().Be(0);
    }

    [Fact]
    public async Task GenerateStockMovementReport_WithNoMovements_ReturnsEmptyReport()
    {
        // Arrange
        _context.StockMovements.RemoveRange(_context.StockMovements);
        await _context.SaveChangesAsync();

        var filter = new InventoryReportFilterDto();

        // Act
        var result = await _service.GenerateStockMovementReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalMovements.Should().Be(0);
        result.Movements.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateInventoryValuationReport_WithNoCategories_HandlesGracefully()
    {
        // Arrange
        var productWithoutCategory = new Product
        {
            Id = 10,
            Name = "Produto sem Categoria",
            Sku = "NO-CAT",
            CategoryId = null,
            CostPrice = 10,
            SalePrice = 20,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var stockForProduct = new Stock
        {
            Id = 10,
            ProductId = 10,
            WarehouseId = 1,
            Quantity = 5,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Products.Add(productWithoutCategory);
        _context.Stocks.Add(stockForProduct);
        await _context.SaveChangesAsync();

        var filter = new InventoryReportFilterDto();

        // Act
        var result = await _service.GenerateInventoryValuationReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        // Should still include categorized products
        result.Categories.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateStockLevelsReport_WithInactiveProducts_ExcludesInactive()
    {
        // Arrange
        var inactiveProduct = _context.Products.First();
        inactiveProduct.IsActive = false;
        await _context.SaveChangesAsync();

        var filter = new InventoryReportFilterDto();

        // Act
        var result = await _service.GenerateStockLevelsReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalProducts.Should().Be(2); // Only active products
    }

    [Fact]
    public async Task GenerateStockMovementReport_WithMultipleFilters_AppliesAllFilters()
    {
        // Arrange
        var filter = new InventoryReportFilterDto
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            MovementType = "Saída",
            WarehouseId = 1
        };

        // Act
        var result = await _service.GenerateStockMovementReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.Movements.Should().OnlyContain(m => 
            m.Type == "Saída" &&
            m.WarehouseId == 1 &&
            m.MovementDate >= filter.StartDate &&
            m.MovementDate <= filter.EndDate);
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
