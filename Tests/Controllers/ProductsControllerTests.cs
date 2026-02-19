using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using erp.Controllers;
using erp.DTOs.Inventory;
using erp.Services.Inventory;

namespace erp.Tests.Controllers;

/// <summary>
/// Testes unitários completos para o controlador de produtos
/// Cobre CRUD, busca avançada, operações em lote e alertas de estoque
/// </summary>
public class ProductsControllerTests
{
    private readonly Mock<IInventoryService> _mockInventoryService;
    private readonly Mock<ILogger<ProductsController>> _mockLogger;
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _mockInventoryService = new Mock<IInventoryService>();
        _mockLogger = new Mock<ILogger<ProductsController>>();
        
        _controller = new ProductsController(
            _mockInventoryService.Object,
            _mockLogger.Object
        );

        // Setup user context
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "test@test.com"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    #region CRUD Tests

    [Fact]
    public async Task CreateProduct_WithValidData_ReturnsCreatedResult()
    {
        // Arrange
        var createDto = new CreateProductDto
        {
            Sku = "PROD-001",
            Name = "Produto Teste",
            Description = "Descrição do produto",
            CategoryId = 1,
            SalePrice = 100.00m,
            CostPrice = 50.00m,
            MinimumStock = 5,
            MaximumStock = 100,
            IsActive = true
        };

        var expectedProduct = new ProductDto
        {
            Id = 1,
            Sku = createDto.Sku,
            Name = createDto.Name,
            Description = createDto.Description,
            CategoryId = createDto.CategoryId,
            CategoryName = "Categoria Teste",
            SalePrice = createDto.SalePrice,
            CostPrice = createDto.CostPrice,
            CurrentStock = 0,
            MinimumStock = createDto.MinimumStock,
            MaximumStock = createDto.MaximumStock,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _mockInventoryService
            .Setup(s => s.CreateProductAsync(It.IsAny<CreateProductDto>(), It.IsAny<int>()))
            .ReturnsAsync(expectedProduct);

        // Act
        var result = await _controller.CreateProduct(createDto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var product = createdResult.Value as ProductDto;
        product.Should().NotBeNull();
        product!.Sku.Should().Be("PROD-001");
        product.Name.Should().Be("Produto Teste");
        product.SalePrice.Should().Be(100.00m);
    }

    [Fact]
    public async Task CreateProduct_WithDuplicateSku_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateProductDto
        {
            Sku = "PROD-001",
            Name = "Produto Teste",
            CategoryId = 1,
            SalePrice = 100.00m,
            CostPrice = 50.00m
        };

        _mockInventoryService
            .Setup(s => s.CreateProductAsync(It.IsAny<CreateProductDto>(), It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("SKU já cadastrado"));

        // Act
        var result = await _controller.CreateProduct(createDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateProduct_WithCompleteTaxInfo_ReturnsCreatedResult()
    {
        // Arrange
        var createDto = new CreateProductDto
        {
            Sku = "PROD-TAX",
            Name = "Produto com Impostos",
            CategoryId = 1,
            SalePrice = 100.00m,
            CostPrice = 50.00m,
            NcmCode = "12345678",
            CestCode = "1234567",
            IcmsRate = 18,
            IpiRate = 5,
            PisRate = 1.65m,
            CofinsRate = 7.6m
        };

        var expectedProduct = new ProductDto
        {
            Id = 1,
            Sku = createDto.Sku,
            Name = createDto.Name,
            NcmCode = createDto.NcmCode,
            CestCode = createDto.CestCode,
            IcmsRate = createDto.IcmsRate,
            IpiRate = createDto.IpiRate,
            PisRate = createDto.PisRate,
            CofinsRate = createDto.CofinsRate
        };

        _mockInventoryService
            .Setup(s => s.CreateProductAsync(It.IsAny<CreateProductDto>(), It.IsAny<int>()))
            .ReturnsAsync(expectedProduct);

        // Act
        var result = await _controller.CreateProduct(createDto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var product = createdResult.Value as ProductDto;
        product.Should().NotBeNull();
        product!.NcmCode.Should().Be("12345678");
        product.IcmsRate.Should().Be(18);
    }

    [Fact]
    public async Task GetProductById_WithExistingId_ReturnsOk()
    {
        // Arrange
        var productId = 1;
        var expectedProduct = new ProductDto
        {
            Id = productId,
            Sku = "PROD-001",
            Name = "Produto Teste",
            SalePrice = 100.00m,
            CurrentStock = 10,
            IsActive = true
        };

        _mockInventoryService
            .Setup(s => s.GetProductByIdAsync(productId))
            .ReturnsAsync(expectedProduct);

        // Act
        var result = await _controller.GetProductById(productId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var product = okResult.Value as ProductDto;
        product.Should().NotBeNull();
        product!.Id.Should().Be(productId);
    }

    [Fact]
    public async Task GetProductById_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var productId = 999;
        _mockInventoryService
            .Setup(s => s.GetProductByIdAsync(productId))
            .ReturnsAsync((ProductDto?)null);

        // Act
        var result = await _controller.GetProductById(productId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetProductBySku_WithExistingSku_ReturnsOk()
    {
        // Arrange
        var sku = "PROD-001";
        var expectedProduct = new ProductDto
        {
            Id = 1,
            Sku = sku,
            Name = "Produto Teste",
            SalePrice = 100.00m,
            IsActive = true
        };

        _mockInventoryService
            .Setup(s => s.GetProductBySkuAsync(sku))
            .ReturnsAsync(expectedProduct);

        // Act
        var result = await _controller.GetProductBySku(sku);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var product = okResult.Value as ProductDto;
        product.Should().NotBeNull();
        product!.Sku.Should().Be(sku);
    }

    [Fact]
    public async Task GetProductBySku_WithNonExistingSku_ReturnsNotFound()
    {
        // Arrange
        var sku = "INVALID-SKU";
        _mockInventoryService
            .Setup(s => s.GetProductBySkuAsync(sku))
            .ReturnsAsync((ProductDto?)null);

        // Act
        var result = await _controller.GetProductBySku(sku);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateProduct_WithValidData_ReturnsOk()
    {
        // Arrange
        var productId = 1;
        var updateDto = new UpdateProductDto
        {
            Id = productId,
            Sku = "PROD-001",
            Name = "Produto Atualizado",
            Description = "Nova descrição",
            CategoryId = 1,
            SalePrice = 150.00m,
            CostPrice = 75.00m,
            IsActive = true
        };

        var updatedProduct = new ProductDto
        {
            Id = productId,
            Sku = updateDto.Sku,
            Name = updateDto.Name,
            Description = updateDto.Description,
            SalePrice = updateDto.SalePrice,
            IsActive = updateDto.IsActive
        };

        _mockInventoryService
            .Setup(s => s.UpdateProductAsync(It.IsAny<UpdateProductDto>()))
            .ReturnsAsync(updatedProduct);

        // Act
        var result = await _controller.UpdateProduct(productId, updateDto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var product = okResult.Value as ProductDto;
        product.Should().NotBeNull();
        product!.Name.Should().Be("Produto Atualizado");
        product.SalePrice.Should().Be(150.00m);
    }

    [Fact]
    public async Task UpdateProduct_WithMismatchedId_ReturnsBadRequest()
    {
        // Arrange
        var urlId = 1;
        var updateDto = new UpdateProductDto
        {
            Id = 2, // ID diferente do da URL
            Name = "Produto Atualizado",
            Sku = "PROD-001",
            CategoryId = 1
        };

        // Act
        var result = await _controller.UpdateProduct(urlId, updateDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteProduct_WithExistingId_ReturnsNoContent()
    {
        // Arrange
        var productId = 1;
        _mockInventoryService
            .Setup(s => s.DeleteProductAsync(productId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteProduct(productId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteProduct_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var productId = 999;
        _mockInventoryService
            .Setup(s => s.DeleteProductAsync(productId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteProduct(productId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task SearchProducts_WithSearchTerm_ReturnsFilteredResults()
    {
        // Arrange
        var searchDto = new ProductSearchDto
        {
            SearchTerm = "Teste",
            Page = 1,
            PageSize = 20,
            SortBy = "Name",
            SortDescending = false
        };

        var products = new List<ProductDto>
        {
            new ProductDto { Id = 1, Name = "Produto Teste 1", Sku = "PROD-001" },
            new ProductDto { Id = 2, Name = "Produto Teste 2", Sku = "PROD-002" }
        };

        _mockInventoryService
            .Setup(s => s.SearchProductsAsync(It.IsAny<ProductSearchDto>()))
            .ReturnsAsync((products, 2));

        // Act
        var result = await _controller.SearchProducts(searchDto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProducts_WithDefaultParameters_ReturnsPagedResults()
    {
        // Arrange
        var products = new List<ProductDto>
        {
            new ProductDto { Id = 1, Name = "Produto 1" },
            new ProductDto { Id = 2, Name = "Produto 2" }
        };

        _mockInventoryService
            .Setup(s => s.SearchProductsAsync(It.IsAny<ProductSearchDto>()))
            .ReturnsAsync((products, 2));

        // Act
        var result = await _controller.GetProducts(search: null, status: null, categoryId: null, lowStock: null, sortBy: null, sortDescending: false, page: 1, pageSize: 20);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SearchProducts_WithCategoryFilter_ReturnsFilteredProducts()
    {
        // Arrange
        var searchDto = new ProductSearchDto
        {
            CategoryId = 1,
            Page = 1,
            PageSize = 20
        };

        var products = new List<ProductDto>
        {
            new ProductDto { Id = 1, Name = "Produto Categoria 1", CategoryId = 1 }
        };

        _mockInventoryService
            .Setup(s => s.SearchProductsAsync(It.IsAny<ProductSearchDto>()))
            .ReturnsAsync((products, 1));

        // Act
        var result = await _controller.SearchProducts(searchDto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchProducts_WithLowStockFilter_ReturnsLowStockProducts()
    {
        // Arrange
        var searchDto = new ProductSearchDto
        {
            LowStock = true,
            Page = 1,
            PageSize = 20
        };

        var products = new List<ProductDto>
        {
            new ProductDto { Id = 1, Name = "Produto 1", CurrentStock = 3, MinimumStock = 10 },
            new ProductDto { Id = 2, Name = "Produto 2", CurrentStock = 5, MinimumStock = 15 }
        };

        _mockInventoryService
            .Setup(s => s.SearchProductsAsync(It.IsAny<ProductSearchDto>()))
            .ReturnsAsync((products, 2));

        // Act
        var result = await _controller.SearchProducts(searchDto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    #endregion

    #region Bulk Operations Tests

    [Fact]
    public async Task BulkUpdatePrices_WithPercentageIncrease_ReturnsOk()
    {
        // Arrange
        var bulkUpdateDto = new BulkUpdatePriceDto
        {
            ProductIds = new List<int> { 1, 2, 3 },
            SalePriceAdjustment = 10,
            AdjustmentIsPercentage = true
        };

        _mockInventoryService
            .Setup(s => s.BulkUpdatePricesAsync(It.IsAny<BulkUpdatePriceDto>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.BulkUpdatePrices(bulkUpdateDto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task BulkUpdatePrices_WithFixedValue_ReturnsOk()
    {
        // Arrange
        var bulkUpdateDto = new BulkUpdatePriceDto
        {
            ProductIds = new List<int> { 1, 2, 3 },
            SalePriceAdjustment = 50,
            AdjustmentIsPercentage = false
        };

        _mockInventoryService
            .Setup(s => s.BulkUpdatePricesAsync(It.IsAny<BulkUpdatePriceDto>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.BulkUpdatePrices(bulkUpdateDto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task BulkUpdatePrices_WithEmptyList_ReturnsBadRequest()
    {
        // Arrange
        var bulkUpdateDto = new BulkUpdatePriceDto
        {
            ProductIds = new List<int>(),
            SalePriceAdjustment = 10,
            AdjustmentIsPercentage = true
        };

        // Act
        var result = await _controller.BulkUpdatePrices(bulkUpdateDto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task BulkUpdatePrices_WithBothCostAndSaleAdjustment_ReturnsOk()
    {
        // Arrange
        var bulkUpdateDto = new BulkUpdatePriceDto
        {
            ProductIds = new List<int> { 1, 2 },
            CostPriceAdjustment = 5,
            SalePriceAdjustment = 10,
            AdjustmentIsPercentage = true
        };

        _mockInventoryService
            .Setup(s => s.BulkUpdatePricesAsync(It.IsAny<BulkUpdatePriceDto>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.BulkUpdatePrices(bulkUpdateDto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region Alert Tests

    [Fact]
    public async Task GetStockAlerts_ReturnsAllAlerts()
    {
        // Arrange
        var alerts = new List<StockAlertDto>
        {
            new StockAlertDto { ProductId = 1, AlertType = "LowStock" },
            new StockAlertDto { ProductId = 2, AlertType = "LowStock" },
            new StockAlertDto { ProductId = 3, AlertType = "LowStock" },
            new StockAlertDto { ProductId = 4, AlertType = "LowStock" },
            new StockAlertDto { ProductId = 5, AlertType = "LowStock" }
        };

        _mockInventoryService
            .Setup(s => s.GetStockAlertsAsync())
            .ReturnsAsync(alerts);

        // Act
        var result = await _controller.GetStockAlerts();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAlerts = okResult.Value as List<StockAlertDto>;
        returnedAlerts.Should().NotBeNull();
        returnedAlerts.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetLowStockProducts_ReturnsProductsBelowMinimum()
    {
        // Arrange
        var alerts = new List<StockAlertDto>
        {
            new StockAlertDto { ProductId = 1, ProductName = "Produto 1", CurrentStock = 3, MinimumStock = 10 },
            new StockAlertDto { ProductId = 2, ProductName = "Produto 2", CurrentStock = 5, MinimumStock = 15 }
        };

        _mockInventoryService
            .Setup(s => s.GetLowStockProductsAsync())
            .ReturnsAsync(alerts);

        // Act
        var result = await _controller.GetLowStockProducts();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAlerts = okResult.Value as List<StockAlertDto>;
        returnedAlerts.Should().NotBeNull();
        returnedAlerts.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOverstockProducts_ReturnsProductsAboveMaximum()
    {
        // Arrange
        var alerts = new List<StockAlertDto>
        {
            new StockAlertDto { ProductId = 1, ProductName = "Produto 1", CurrentStock = 150 },
            new StockAlertDto { ProductId = 2, ProductName = "Produto 2", CurrentStock = 250 }
        };

        _mockInventoryService
            .Setup(s => s.GetOverstockProductsAsync())
            .ReturnsAsync(alerts);

        // Act
        var result = await _controller.GetOverstockProducts();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAlerts = okResult.Value as List<StockAlertDto>;
        returnedAlerts.Should().NotBeNull();
        returnedAlerts.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetInactiveProducts_WithCustomDays_ReturnsInactiveProducts()
    {
        // Arrange
        var days = 60;
        var alerts = new List<StockAlertDto>
        {
            new StockAlertDto { ProductId = 1, ProductName = "Produto Inativo 1", AlertType = "Inactive" },
            new StockAlertDto { ProductId = 2, ProductName = "Produto Inativo 2", AlertType = "Inactive" }
        };

        _mockInventoryService
            .Setup(s => s.GetInactiveProductsAsync(days))
            .ReturnsAsync(alerts);

        // Act
        var result = await _controller.GetInactiveProducts(days);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAlerts = okResult.Value as List<StockAlertDto>;
        returnedAlerts.Should().NotBeNull();
        returnedAlerts.Should().HaveCount(2);
        _mockInventoryService.Verify(s => s.GetInactiveProductsAsync(days), Times.Once);
    }

    [Fact]
    public async Task GetInactiveProducts_WithInvalidDays_UsesDefaultValue()
    {
        // Arrange
        var alerts = new List<StockAlertDto>();

        _mockInventoryService
            .Setup(s => s.GetInactiveProductsAsync(90))
            .ReturnsAsync(alerts);

        // Act
        var result = await _controller.GetInactiveProducts(-10);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockInventoryService.Verify(s => s.GetInactiveProductsAsync(90), Times.Once);
    }

    #endregion
}
