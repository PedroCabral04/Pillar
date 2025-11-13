using System.Security.Claims;
using erp.Controllers;
using erp.DTOs.Dashboard;
using erp.Services.DashboardCustomization;
using Microsoft.AspNetCore.Http;

namespace erp.Tests.Controllers;

/// <summary>
/// Testes unitários para o controlador de Dashboard Layout
/// </summary>
public class DashboardLayoutControllerTests
{
    private readonly Mock<IDashboardLayoutService> _mockLayoutService;
    private readonly LayoutController _controller;
    private readonly string _testUserId = "1";

    public DashboardLayoutControllerTests()
    {
        _mockLayoutService = new Mock<IDashboardLayoutService>();
        _controller = new LayoutController(_mockLayoutService.Object);

        // Setup user context
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId),
            new Claim(ClaimTypes.Name, "test@test.com"),
            new Claim(ClaimTypes.Role, "User")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    #region GetLayout Tests

    [Fact]
    public async Task GetLayout_ReturnsOkWithLayout()
    {
        // Arrange
        var expectedLayout = new DashboardLayout
        {
            UserId = _testUserId,
            LayoutType = "grid",
            Columns = 3,
            Widgets = new List<WidgetConfiguration>
            {
                new WidgetConfiguration
                {
                    WidgetId = "widget1",
                    ProviderKey = "sales",
                    WidgetKey = "revenue"
                }
            }
        };

        _mockLayoutService
            .Setup(x => x.GetUserLayoutAsync(_testUserId))
            .ReturnsAsync(expectedLayout);

        // Act
        var result = await _controller.GetLayout();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var layout = okResult.Value as DashboardLayout;
        layout.Should().NotBeNull();
        layout!.UserId.Should().Be(_testUserId);
        layout.Widgets.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetLayout_WithoutUserId_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

        // Act
        var result = await _controller.GetLayout();

        // Assert
        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    #endregion

    #region SaveLayout Tests

    [Fact]
    public async Task SaveLayout_WithValidData_ReturnsOk()
    {
        // Arrange
        var request = new SaveLayoutRequest
        {
            LayoutType = "grid",
            Columns = 3,
            Widgets = new List<WidgetConfiguration>
            {
                new WidgetConfiguration { WidgetId = "widget1", ProviderKey = "sales", WidgetKey = "revenue" }
            }
        };

        _mockLayoutService
            .Setup(x => x.SaveUserLayoutAsync(_testUserId, It.IsAny<DashboardLayout>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.SaveLayout(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockLayoutService.Verify(x => x.SaveUserLayoutAsync(
            _testUserId,
            It.Is<DashboardLayout>(l => l.LayoutType == request.LayoutType && l.Columns == request.Columns)),
            Times.Once);
    }

    [Fact]
    public async Task SaveLayout_WithoutUserId_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();
        var request = new SaveLayoutRequest { Widgets = new List<WidgetConfiguration>() };

        // Act
        var result = await _controller.SaveLayout(request);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    #endregion

    #region ResetLayout Tests

    [Fact]
    public async Task ResetLayout_ReturnsOk()
    {
        // Arrange
        _mockLayoutService
            .Setup(x => x.ResetToDefaultAsync(_testUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ResetLayout();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockLayoutService.Verify(x => x.ResetToDefaultAsync(_testUserId), Times.Once);
    }

    [Fact]
    public async Task ResetLayout_WithoutUserId_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

        // Act
        var result = await _controller.ResetLayout();

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    #endregion

    #region GetWidgetCatalog Tests

    [Fact]
    public void GetWidgetCatalog_ReturnsOkWithCatalog()
    {
        // Arrange
        var catalog = new List<WidgetCatalogItem>
        {
            new WidgetCatalogItem
            {
                ProviderKey = "sales",
                WidgetKey = "revenue",
                Title = "Revenue",
                Description = "Total revenue"
            }
        };

        _mockLayoutService
            .Setup(x => x.GetAvailableWidgets(It.IsAny<string[]>()))
            .Returns(catalog);

        // Act
        var result = _controller.GetWidgetCatalog();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCatalog = okResult.Value as List<WidgetCatalogItem>;
        returnedCatalog.Should().NotBeNull();
        returnedCatalog.Should().HaveCount(1);
    }

    #endregion

    #region AddWidget Tests

    [Fact]
    public async Task AddWidget_WithValidData_ReturnsOk()
    {
        // Arrange
        var request = new AddWidgetRequest
        {
            ProviderKey = "sales",
            WidgetKey = "revenue",
            Row = 0,
            Column = 0
        };

        var addedWidget = new WidgetConfiguration
        {
            WidgetId = "widget1",
            ProviderKey = request.ProviderKey,
            WidgetKey = request.WidgetKey
        };

        _mockLayoutService
            .Setup(x => x.AddWidgetAsync(_testUserId, request.ProviderKey, request.WidgetKey, request.Row, request.Column))
            .ReturnsAsync(addedWidget);

        // Act
        var result = await _controller.AddWidget(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var widget = okResult.Value as WidgetConfiguration;
        widget.Should().NotBeNull();
        widget!.ProviderKey.Should().Be(request.ProviderKey);
    }

    [Fact]
    public async Task AddWidget_WhenAlreadyExists_ReturnsBadRequest()
    {
        // Arrange
        var request = new AddWidgetRequest
        {
            ProviderKey = "sales",
            WidgetKey = "revenue",
            Row = 0,
            Column = 0
        };

        _mockLayoutService
            .Setup(x => x.AddWidgetAsync(_testUserId, request.ProviderKey, request.WidgetKey, request.Row, request.Column))
            .ReturnsAsync((WidgetConfiguration?)null);

        // Act
        var result = await _controller.AddWidget(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task AddWidget_WithoutUserId_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();
        var request = new AddWidgetRequest
        {
            ProviderKey = "sales",
            WidgetKey = "revenue"
        };

        // Act
        var result = await _controller.AddWidget(request);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    #endregion

    #region RemoveWidget Tests

    [Fact]
    public async Task RemoveWidget_WithValidId_ReturnsOk()
    {
        // Arrange
        var widgetId = "widget1";
        _mockLayoutService
            .Setup(x => x.RemoveWidgetAsync(_testUserId, widgetId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.RemoveWidget(widgetId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RemoveWidget_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var widgetId = "nonexisting";
        _mockLayoutService
            .Setup(x => x.RemoveWidgetAsync(_testUserId, widgetId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.RemoveWidget(widgetId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region UpdateWidget Tests

    [Fact]
    public async Task UpdateWidget_WithValidData_ReturnsOk()
    {
        // Arrange
        var widgetId = "widget1";
        var request = new UpdateWidgetRequest
        {
            Row = 1,
            Column = 2
        };

        _mockLayoutService
            .Setup(x => x.UpdateWidgetAsync(_testUserId, widgetId, request))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateWidget(widgetId, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateWidget_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var widgetId = "nonexisting";
        var request = new UpdateWidgetRequest();

        _mockLayoutService
            .Setup(x => x.UpdateWidgetAsync(_testUserId, widgetId, request))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateWidget(widgetId, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region ReorderWidgets Tests

    [Fact]
    public async Task ReorderWidgets_WithValidOrder_ReturnsOk()
    {
        // Arrange
        var widgetOrder = new List<string> { "widget1", "widget2", "widget3" };

        _mockLayoutService
            .Setup(x => x.ReorderWidgetsAsync(_testUserId, widgetOrder))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ReorderWidgets(widgetOrder);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ReorderWidgets_WithFailure_ReturnsBadRequest()
    {
        // Arrange
        var widgetOrder = new List<string> { "widget1", "widget2" };

        _mockLayoutService
            .Setup(x => x.ReorderWidgetsAsync(_testUserId, widgetOrder))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ReorderWidgets(widgetOrder);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion
}
