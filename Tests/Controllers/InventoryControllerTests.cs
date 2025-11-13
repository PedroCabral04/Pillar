using System.Security.Claims;
using erp.Controllers;
using erp.DTOs.Inventory;
using erp.Services.Inventory;
using Microsoft.AspNetCore.Http;

namespace erp.Tests.Controllers;

public class InventoryControllerTests
{
    private readonly Mock<IStockCountService> _mockStockCountService;
    private readonly Mock<IInventoryService> _mockInventoryService;
    private readonly Mock<ILogger<InventoryController>> _mockLogger;
    private readonly InventoryController _controller;

    public InventoryControllerTests()
    {
        _mockStockCountService = new Mock<IStockCountService>();
        _mockInventoryService = new Mock<IInventoryService>();
        _mockLogger = new Mock<ILogger<InventoryController>>();
        
        _controller = new InventoryController(
            _mockStockCountService.Object,
            _mockInventoryService.Object,
            _mockLogger.Object
        );

        // Setup user context
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "test@test.com")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    #region Stock Count Tests

    [Fact]
    public async Task CreateStockCount_WithValidData_ReturnsCreatedResult()
    {
        // Arrange
        var createDto = new CreateStockCountDto
        {
            Description = "Contagem Mensal",
            WarehouseId = 1
        };

        var expectedCount = new StockCountDto
        {
            Id = 1,
            CountNumber = "CNT-001",
            Status = 0, // 0 = Pendente
            StatusName = "Pendente",
            CountDate = DateTime.UtcNow,
            WarehouseId = createDto.WarehouseId,
            CreatedByUserId = 1
        };

        _mockStockCountService
            .Setup(s => s.CreateCountAsync(It.IsAny<CreateStockCountDto>(), It.IsAny<int>()))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _controller.CreateStockCount(createDto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.Value.Should().BeEquivalentTo(expectedCount);
        _mockStockCountService.Verify(s => s.CreateCountAsync(It.IsAny<CreateStockCountDto>(), 1), Times.Once);
    }

    [Fact]
    public async Task GetStockCountById_WithExistingId_ReturnsOk()
    {
        // Arrange
        var countId = 1;
        var expectedCount = new StockCountDto
        {
            Id = countId,
            CountNumber = "CNT-TEST",
            Status = 0,
            StatusName = "Pendente"
        };

        _mockStockCountService
            .Setup(s => s.GetCountByIdAsync(countId))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _controller.GetStockCountById(countId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedCount);
    }

    [Fact]
    public async Task GetStockCountById_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var countId = 999;
        _mockStockCountService
            .Setup(s => s.GetCountByIdAsync(countId))
            .ReturnsAsync((StockCountDto?)null);

        // Act
        var result = await _controller.GetStockCountById(countId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetActiveCounts_ReturnsAllActiveCountsSuccessfully()
    {
        // Arrange
        var activeCounts = new List<StockCountDto>
        {
            new StockCountDto { Id = 1, CountNumber = "CNT-001", Status = 0, StatusName = "Pendente" },
            new StockCountDto { Id = 2, CountNumber = "CNT-002", Status = 1, StatusName = "EmAndamento" }
        };

        _mockStockCountService
            .Setup(s => s.GetActiveCountsAsync())
            .ReturnsAsync(activeCounts);

        // Act
        var result = await _controller.GetActiveCounts();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(activeCounts);
    }

    [Fact]
    public async Task AddItemToCount_WithValidData_ReturnsOk()
    {
        // Arrange
        var countId = 1;
        var itemDto = new AddStockCountItemDto
        {
            StockCountId = countId,
            ProductId = 1,
            PhysicalStock = 100
        };

        var product = new ProductDto { Id = 1, Name = "Produto Teste", CurrentStock = 95 };

        var updatedCount = new StockCountDto
        {
            Id = countId,
            CountNumber = "CNT-TEST",
            Status = 1,
            StatusName = "EmAndamento"
        };

        _mockInventoryService
            .Setup(s => s.GetProductByIdAsync(itemDto.ProductId))
            .ReturnsAsync(product);

        _mockStockCountService
            .Setup(s => s.AddItemToCountAsync(It.IsAny<AddStockCountItemDto>()))
            .ReturnsAsync(updatedCount);

        // Act
        var result = await _controller.AddItemToCount(countId, itemDto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(updatedCount);
    }

    [Fact]
    public async Task AddItemToCount_WithNonExistingProduct_ReturnsNotFound()
    {
        // Arrange
        var countId = 1;
        var itemDto = new AddStockCountItemDto
        {
            StockCountId = countId,
            ProductId = 999,
            PhysicalStock = 100
        };

        _mockInventoryService
            .Setup(s => s.GetProductByIdAsync(itemDto.ProductId))
            .ReturnsAsync((ProductDto?)null);

        // Act
        var result = await _controller.AddItemToCount(countId, itemDto);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ApproveCount_WithValidData_ReturnsOk()
    {
        // Arrange
        var countId = 1;
        var approveDto = new ApproveStockCountDto
        {
            StockCountId = countId,
            Notes = "Contagem aprovada"
        };

        var approvedCount = new StockCountDto
        {
            Id = countId,
            CountNumber = "CNT-TEST",
            Status = 2,
            StatusName = "Aprovada",
            ClosedDate = DateTime.UtcNow
        };

        _mockStockCountService
            .Setup(s => s.ApproveCountAsync(It.IsAny<ApproveStockCountDto>(), It.IsAny<int>()))
            .ReturnsAsync(approvedCount);

        // Act
        var result = await _controller.ApproveCount(countId, approveDto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(approvedCount);
    }

    [Fact]
    public async Task ApproveCount_WithEmptyCount_ReturnsBadRequest()
    {
        // Arrange
        var countId = 1;
        var approveDto = new ApproveStockCountDto();

        _mockStockCountService
            .Setup(s => s.ApproveCountAsync(It.IsAny<ApproveStockCountDto>(), It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("Contagem sem itens não pode ser aprovada"));

        // Act
        var result = await _controller.ApproveCount(countId, approveDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CancelCount_WithValidId_ReturnsOk()
    {
        // Arrange
        var countId = 1;
        _mockStockCountService
            .Setup(s => s.CancelCountAsync(countId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CancelCount(countId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CancelCount_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var countId = 999;
        _mockStockCountService
            .Setup(s => s.CancelCountAsync(countId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.CancelCount(countId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Alerts & Reports Tests

    [Fact]
    public async Task GetAlerts_ReturnsAllAlertsSuccessfully()
    {
        // Arrange
        var alerts = new StockAlertsDto
        {
            LowStockCount = 5,
            OverstockCount = 3,
            InactiveProductsCount = 10
        };

        var alertsList = new List<StockAlertDto>
        {
            new StockAlertDto { ProductId = 1, AlertType = "LowStock" },
            new StockAlertDto { ProductId = 2, AlertType = "LowStock" },
            new StockAlertDto { ProductId = 3, AlertType = "LowStock" },
            new StockAlertDto { ProductId = 4, AlertType = "LowStock" },
            new StockAlertDto { ProductId = 5, AlertType = "LowStock" }
        };
        
        _mockInventoryService
            .Setup(s => s.GetStockAlertsAsync())
            .ReturnsAsync(alertsList);

        // Act
        var result = await _controller.GetAlerts();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var resultList = okResult.Value as List<StockAlertDto>;
        resultList.Should().NotBeNull();
        resultList.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetLowStockProducts_ReturnsProductsSuccessfully()
    {
        // Arrange
        var alerts = new List<StockAlertDto>
        {
            new StockAlertDto { ProductId = 1, ProductName = "Produto 1", CurrentStock = 5, MinimumStock = 10 },
            new StockAlertDto { ProductId = 2, ProductName = "Produto 2", CurrentStock = 2, MinimumStock = 15 }
        };

        _mockInventoryService
            .Setup(s => s.GetLowStockProductsAsync())
            .ReturnsAsync(alerts);

        // Act
        var result = await _controller.GetLowStockProducts();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var resultAlerts = okResult.Value as List<StockAlertDto>;
        resultAlerts.Should().NotBeNull();
        resultAlerts.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOverstockProducts_ReturnsProductsSuccessfully()
    {
        // Arrange
        var alerts = new List<StockAlertDto>
        {
            new StockAlertDto { ProductId = 1, ProductName = "Produto 1", CurrentStock = 150 },
            new StockAlertDto { ProductId = 2, ProductName = "Produto 2", CurrentStock = 200 }
        };

        _mockInventoryService
            .Setup(s => s.GetOverstockProductsAsync())
            .ReturnsAsync(alerts);

        // Act
        var result = await _controller.GetOverstockProducts();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var resultAlerts = okResult.Value as List<StockAlertDto>;
        resultAlerts.Should().NotBeNull();
        resultAlerts.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetInactiveProducts_WithDefaultDays_ReturnsProductsSuccessfully()
    {
        // Arrange
        var alerts = new List<StockAlertDto>
        {
            new StockAlertDto { ProductId = 1, ProductName = "Produto Inativo 1", AlertType = "Inactive" },
            new StockAlertDto { ProductId = 2, ProductName = "Produto Inativo 2", AlertType = "Inactive" }
        };

        _mockInventoryService
            .Setup(s => s.GetInactiveProductsAsync(90))
            .ReturnsAsync(alerts);

        // Act
        var result = await _controller.GetInactiveProducts(90);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var resultAlerts = okResult.Value as List<StockAlertDto>;
        resultAlerts.Should().NotBeNull();
        resultAlerts.Should().HaveCount(2);
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
        var result = await _controller.GetInactiveProducts(0);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockInventoryService.Verify(s => s.GetInactiveProductsAsync(90), Times.Once);
    }

    #endregion
}
