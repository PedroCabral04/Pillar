using System;
using System.Collections.Generic;
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
/// Testes unitários para o controlador de movimentações de estoque
/// Cobre entradas, saídas, ajustes e histórico de movimentações
/// </summary>
public class StockMovementsControllerTests
{
    private readonly Mock<IStockMovementService> _mockStockMovementService;
    private readonly Mock<IInventoryService> _mockInventoryService;
    private readonly Mock<ILogger<StockMovementsController>> _mockLogger;
    private readonly StockMovementsController _controller;

    public StockMovementsControllerTests()
    {
        _mockStockMovementService = new Mock<IStockMovementService>();
        _mockInventoryService = new Mock<IInventoryService>();
        _mockLogger = new Mock<ILogger<StockMovementsController>>();
        
        _controller = new StockMovementsController(
            _mockStockMovementService.Object,
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

    #region Generic Movement Tests

    [Fact]
    public async Task CreateMovement_WithValidData_ReturnsCreatedResult()
    {
        // Arrange
        var createDto = new CreateStockMovementDto
        {
            ProductId = 1,
            Quantity = 10,
            Type = 1, // 1 = Entrada
            UnitCost = 50.00m,
            DocumentNumber = "NF-001",
            Notes = "Compra de produtos"
        };

        var product = new ProductDto { Id = 1, Name = "Produto Teste" };
        
        var expectedMovement = new StockMovementDto
        {
            Id = 1,
            ProductId = createDto.ProductId,
            ProductName = "Produto Teste",
            Quantity = createDto.Quantity,
            Type = createDto.Type,
            TypeName = "Entrada",
            UnitCost = createDto.UnitCost,
            DocumentNumber = createDto.DocumentNumber,
            Notes = createDto.Notes,
            MovementDate = DateTime.UtcNow
        };

        _mockInventoryService
            .Setup(s => s.GetProductByIdAsync(createDto.ProductId))
            .ReturnsAsync(product);

        _mockStockMovementService
            .Setup(s => s.CreateMovementAsync(It.IsAny<CreateStockMovementDto>(), It.IsAny<int>()))
            .ReturnsAsync(expectedMovement);

        // Act
        var result = await _controller.CreateMovement(createDto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var movement = createdResult.Value as StockMovementDto;
        movement.Should().NotBeNull();
        movement!.ProductId.Should().Be(1);
        movement.Quantity.Should().Be(10);
    }

    [Fact]
    public async Task CreateMovement_WithNonExistingProduct_ReturnsNotFound()
    {
        // Arrange
        var createDto = new CreateStockMovementDto
        {
            ProductId = 999,
            Quantity = 10,
            Type = 1 // 1 = Entrada
        };

        _mockInventoryService
            .Setup(s => s.GetProductByIdAsync(createDto.ProductId))
            .ReturnsAsync((ProductDto?)null);

        // Act
        var result = await _controller.CreateMovement(createDto);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetMovementById_WithExistingId_ReturnsOk()
    {
        // Arrange
        var movementId = 1;
        var expectedMovement = new StockMovementDto
        {
            Id = movementId,
            ProductId = 1,
            ProductName = "Produto Teste",
            Quantity = 10,
            Type = 1,
            TypeName = "Entrada"
        };

        _mockStockMovementService
            .Setup(s => s.GetMovementByIdAsync(movementId))
            .ReturnsAsync(expectedMovement);

        // Act
        var result = await _controller.GetMovementById(movementId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var movement = okResult.Value as StockMovementDto;
        movement.Should().NotBeNull();
        movement!.Id.Should().Be(movementId);
    }

    [Fact]
    public async Task GetMovementById_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var movementId = 999;
        _mockStockMovementService
            .Setup(s => s.GetMovementByIdAsync(movementId))
            .ReturnsAsync((StockMovementDto?)null);

        // Act
        var result = await _controller.GetMovementById(movementId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Entry Tests

    [Fact]
    public async Task CreateEntry_WithValidData_ReturnsCreatedResult()
    {
        // Arrange
        var createDto = new CreateStockMovementDto
        {
            ProductId = 1,
            Quantity = 50,
            UnitCost = 45.00m,
            DocumentNumber = "NF-12345",
            Notes = "Entrada de mercadorias",
            WarehouseId = 1
        };

        var product = new ProductDto { Id = 1, Name = "Produto Teste" };
        
        var expectedMovement = new StockMovementDto
        {
            Id = 1,
            ProductId = 1,
            ProductName = "Produto Teste",
            Quantity = 50,
            Type = 1,
            TypeName = "Entrada",
            UnitCost = 45.00m,
            DocumentNumber = "NF-12345",
            MovementDate = DateTime.UtcNow
        };

        _mockInventoryService
            .Setup(s => s.GetProductByIdAsync(1))
            .ReturnsAsync(product);

        _mockStockMovementService
            .Setup(s => s.CreateEntryAsync(
                It.IsAny<int>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int?>()))
            .ReturnsAsync(expectedMovement);

        // Act
        var result = await _controller.CreateEntry(createDto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var movement = createdResult.Value as StockMovementDto;
        movement.Should().NotBeNull();
        movement!.Type.Should().Be(1);
        movement.TypeName.Should().Be("Entrada");
        movement.Quantity.Should().Be(50);
    }

    [Fact]
    public async Task CreateEntry_WithNegativeQuantity_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateStockMovementDto
        {
            ProductId = 1,
            Quantity = -10,
            UnitCost = 50.00m
        };

        var product = new ProductDto { Id = 1 };
        _mockInventoryService.Setup(s => s.GetProductByIdAsync(1)).ReturnsAsync(product);

        _mockStockMovementService
            .Setup(s => s.CreateEntryAsync(
                It.IsAny<int>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int?>()))
            .ThrowsAsync(new InvalidOperationException("Quantidade deve ser positiva"));

        // Act
        var result = await _controller.CreateEntry(createDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Exit Tests

    [Fact]
    public async Task CreateExit_WithValidData_ReturnsCreatedResult()
    {
        // Arrange
        var createDto = new CreateStockMovementDto
        {
            ProductId = 1,
            Quantity = 20,
            DocumentNumber = "SAIDA-001",
            Notes = "Saída para venda",
            WarehouseId = 1
        };

        var product = new ProductDto { Id = 1, Name = "Produto Teste", CurrentStock = 50 };
        
        var expectedMovement = new StockMovementDto
        {
            Id = 1,
            ProductId = 1,
            ProductName = "Produto Teste",
            Quantity = 20,
            Type = 2,
            TypeName = "Saída",
            DocumentNumber = "SAIDA-001",
            MovementDate = DateTime.UtcNow
        };

        _mockInventoryService
            .Setup(s => s.GetProductByIdAsync(1))
            .ReturnsAsync(product);

        _mockStockMovementService
            .Setup(s => s.CreateExitAsync(
                It.IsAny<int>(),
                It.IsAny<decimal>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int?>()))
            .ReturnsAsync(expectedMovement);

        // Act
        var result = await _controller.CreateExit(createDto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var movement = createdResult.Value as StockMovementDto;
        movement.Should().NotBeNull();
        movement!.Type.Should().Be(2);
        movement.TypeName.Should().Be("Saída");
        movement.Quantity.Should().Be(20);
    }

    [Fact]
    public async Task CreateExit_WithInsufficientStock_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateStockMovementDto
        {
            ProductId = 1,
            Quantity = 100,
            DocumentNumber = "SAIDA-002"
        };

        var product = new ProductDto { Id = 1, CurrentStock = 10 };
        _mockInventoryService.Setup(s => s.GetProductByIdAsync(1)).ReturnsAsync(product);

        _mockStockMovementService
            .Setup(s => s.CreateExitAsync(
                It.IsAny<int>(),
                It.IsAny<decimal>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int?>()))
            .ThrowsAsync(new InvalidOperationException("Estoque insuficiente"));

        // Act
        var result = await _controller.CreateExit(createDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateExit_WithNonExistingProduct_ReturnsNotFound()
    {
        // Arrange
        var createDto = new CreateStockMovementDto
        {
            ProductId = 999,
            Quantity = 10
        };

        _mockInventoryService
            .Setup(s => s.GetProductByIdAsync(999))
            .ReturnsAsync((ProductDto?)null);

        // Act
        var result = await _controller.CreateExit(createDto);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Adjustment Tests

    [Fact]
    public async Task CreateAdjustment_WithValidData_ReturnsCreatedResult()
    {
        // Arrange
        var adjustmentDto = new CreateStockAdjustmentDto
        {
            ProductId = 1,
            NewStock = 75,
            Reason = "Ajuste de inventário",
            WarehouseId = 1
        };

        var product = new ProductDto { Id = 1, Name = "Produto Teste", CurrentStock = 70 };
        
        var expectedMovement = new StockMovementDto
        {
            Id = 1,
            ProductId = 1,
            ProductName = "Produto Teste",
            Quantity = 5, // Diferença entre novo estoque e atual
            Type = 3,
            TypeName = "Ajuste",
            Notes = "Ajuste de inventário",
            MovementDate = DateTime.UtcNow
        };

        _mockInventoryService
            .Setup(s => s.GetProductByIdAsync(1))
            .ReturnsAsync(product);

        _mockStockMovementService
            .Setup(s => s.CreateAdjustmentAsync(
                It.IsAny<int>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int?>()))
            .ReturnsAsync(expectedMovement);

        // Act
        var result = await _controller.CreateAdjustment(adjustmentDto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var movement = createdResult.Value as StockMovementDto;
        movement.Should().NotBeNull();
        movement!.Type.Should().Be(3);
        movement.TypeName.Should().Be("Ajuste");
    }

    [Fact]
    public async Task CreateAdjustment_WithNegativeStock_ReturnsBadRequest()
    {
        // Arrange
        var adjustmentDto = new CreateStockAdjustmentDto
        {
            ProductId = 1,
            NewStock = -10,
            Reason = "Ajuste inválido"
        };

        var product = new ProductDto { Id = 1 };
        _mockInventoryService.Setup(s => s.GetProductByIdAsync(1)).ReturnsAsync(product);

        _mockStockMovementService
            .Setup(s => s.CreateAdjustmentAsync(
                It.IsAny<int>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int?>()))
            .ThrowsAsync(new InvalidOperationException("Estoque não pode ser negativo"));

        // Act
        var result = await _controller.CreateAdjustment(adjustmentDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateAdjustment_WithNonExistingProduct_ReturnsNotFound()
    {
        // Arrange
        var adjustmentDto = new CreateStockAdjustmentDto
        {
            ProductId = 999,
            NewStock = 50,
            Reason = "Ajuste"
        };

        _mockInventoryService
            .Setup(s => s.GetProductByIdAsync(999))
            .ReturnsAsync((ProductDto?)null);

        // Act
        var result = await _controller.CreateAdjustment(adjustmentDto);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region History Tests

    [Fact]
    public async Task GetMovementsByProduct_WithExistingProduct_ReturnsMovements()
    {
        // Arrange
        var productId = 1;
        var product = new ProductDto { Id = productId, Name = "Produto Teste" };
        var movements = new List<StockMovementDto>
        {
            new StockMovementDto { Id = 1, ProductId = productId, Type = 1, TypeName = "Entrada", Quantity = 50 },
            new StockMovementDto { Id = 2, ProductId = productId, Type = 2, TypeName = "Saída", Quantity = 20 },
            new StockMovementDto { Id = 3, ProductId = productId, Type = 3, TypeName = "Ajuste", Quantity = 5 }
        };

        _mockInventoryService
            .Setup(s => s.GetProductByIdAsync(productId))
            .ReturnsAsync(product);

        _mockStockMovementService
            .Setup(s => s.GetMovementsByProductAsync(productId, null, null))
            .ReturnsAsync(movements);

        // Act
        var result = await _controller.GetMovementsByProduct(productId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedMovements = okResult.Value as List<StockMovementDto>;
        returnedMovements.Should().NotBeNull();
        returnedMovements.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetMovementsByProduct_WithDateRange_ReturnsFilteredMovements()
    {
        // Arrange
        var productId = 1;
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;
        var product = new ProductDto { Id = productId };
        
        var movements = new List<StockMovementDto>
        {
            new StockMovementDto { Id = 1, ProductId = productId, MovementDate = DateTime.UtcNow.AddDays(-10) },
            new StockMovementDto { Id = 2, ProductId = productId, MovementDate = DateTime.UtcNow.AddDays(-5) }
        };

        _mockInventoryService.Setup(s => s.GetProductByIdAsync(productId)).ReturnsAsync(product);
        _mockStockMovementService
            .Setup(s => s.GetMovementsByProductAsync(productId, startDate, endDate))
            .ReturnsAsync(movements);

        // Act
        var result = await _controller.GetMovementsByProduct(productId, startDate, endDate);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMovementsByProduct_WithNonExistingProduct_ReturnsNotFound()
    {
        // Arrange
        var productId = 999;
        _mockInventoryService
            .Setup(s => s.GetProductByIdAsync(productId))
            .ReturnsAsync((ProductDto?)null);

        // Act
        var result = await _controller.GetMovementsByProduct(productId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetMovementsByDateRange_WithValidRange_ReturnsMovements()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;
        var movements = new List<StockMovementDto>
        {
            new StockMovementDto { Id = 1, MovementDate = DateTime.UtcNow.AddDays(-20) },
            new StockMovementDto { Id = 2, MovementDate = DateTime.UtcNow.AddDays(-10) }
        };

        _mockStockMovementService
            .Setup(s => s.GetMovementsByDateRangeAsync(startDate, endDate, null))
            .ReturnsAsync(movements);

        // Act
        var result = await _controller.GetMovementsByDateRange(startDate, endDate);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedMovements = okResult.Value as List<StockMovementDto>;
        returnedMovements.Should().NotBeNull();
        returnedMovements.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMovementsByDateRange_WithInvalidRange_ReturnsBadRequest()
    {
        // Arrange
        var startDate = DateTime.UtcNow;
        var endDate = DateTime.UtcNow.AddDays(-30); // End antes do start

        // Act
        var result = await _controller.GetMovementsByDateRange(startDate, endDate);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetMovementsByDateRange_WithWarehouseFilter_ReturnsFilteredMovements()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;
        var warehouseId = 1;
        var movements = new List<StockMovementDto>
        {
            new StockMovementDto { Id = 1, WarehouseId = warehouseId }
        };

        _mockStockMovementService
            .Setup(s => s.GetMovementsByDateRangeAsync(startDate, endDate, warehouseId))
            .ReturnsAsync(movements);

        // Act
        var result = await _controller.GetMovementsByDateRange(startDate, endDate, warehouseId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    #endregion
}
