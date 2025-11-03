using erp.Controllers;
using erp.DTOs.Dashboard;
using erp.Services.Dashboard;

namespace erp.Tests.Controllers;

/// <summary>
/// Testes unitários para o controlador de Dashboard
/// </summary>
public class DashboardControllerTests
{
    private readonly Mock<IDashboardRegistry> _mockRegistry;
    private readonly DashboardController _controller;

    public DashboardControllerTests()
    {
        _mockRegistry = new Mock<IDashboardRegistry>();
        _controller = new DashboardController(_mockRegistry.Object);
    }

    #region GetWidgets Tests

    [Fact]
    public void GetWidgets_ReturnsAllWidgets()
    {
        // Arrange
        var widgets = new List<DashboardWidgetDefinition>
        {
            new DashboardWidgetDefinition
            {
                ProviderKey = "sales",
                WidgetKey = "revenue",
                Title = "Revenue",
                Description = "Total revenue"
            },
            new DashboardWidgetDefinition
            {
                ProviderKey = "sales",
                WidgetKey = "orders",
                Title = "Orders",
                Description = "Total orders"
            }
        };

        _mockRegistry.Setup(x => x.ListAll())
            .Returns(widgets);

        // Act
        var result = _controller.GetWidgets();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedWidgets = okResult.Value as List<DashboardWidgetDefinition>;
        returnedWidgets.Should().NotBeNull();
        returnedWidgets.Should().HaveCount(2);
    }

    [Fact]
    public void GetWidgets_WithNoWidgets_ReturnsEmptyList()
    {
        // Arrange
        _mockRegistry.Setup(x => x.ListAll())
            .Returns(new List<DashboardWidgetDefinition>());

        // Act
        var result = _controller.GetWidgets();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedWidgets = okResult.Value as List<DashboardWidgetDefinition>;
        returnedWidgets.Should().NotBeNull();
        returnedWidgets.Should().BeEmpty();
    }

    #endregion

    #region GetWidgetsByProvider Tests

    [Fact]
    public void GetWidgetsByProvider_WithValidProvider_ReturnsWidgets()
    {
        // Arrange
        var providerKey = "sales";
        var widgets = new List<DashboardWidgetDefinition>
        {
            new DashboardWidgetDefinition
            {
                ProviderKey = providerKey,
                WidgetKey = "revenue",
                Title = "Revenue"
            }
        };

        _mockRegistry.Setup(x => x.ListByProvider(providerKey))
            .Returns(widgets);

        // Act
        var result = _controller.GetWidgetsByProvider(providerKey);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedWidgets = okResult.Value as List<DashboardWidgetDefinition>;
        returnedWidgets.Should().NotBeNull();
        returnedWidgets.Should().HaveCount(1);
        returnedWidgets![0].ProviderKey.Should().Be(providerKey);
    }

    [Fact]
    public void GetWidgetsByProvider_WithNonExistingProvider_ReturnsEmptyList()
    {
        // Arrange
        var providerKey = "nonexisting";
        _mockRegistry.Setup(x => x.ListByProvider(providerKey))
            .Returns(new List<DashboardWidgetDefinition>());

        // Act
        var result = _controller.GetWidgetsByProvider(providerKey);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedWidgets = okResult.Value as List<DashboardWidgetDefinition>;
        returnedWidgets.Should().NotBeNull();
        returnedWidgets.Should().BeEmpty();
    }

    #endregion

    #region Query Tests

    [Fact]
    public async Task Query_WithValidProviderAndWidget_ReturnsData()
    {
        // Arrange
        var providerKey = "sales";
        var widgetKey = "revenue";
        var query = new DashboardQuery
        {
            From = DateTime.UtcNow.AddDays(-30),
            To = DateTime.UtcNow
        };

        var definition = new DashboardWidgetDefinition
        {
            ProviderKey = providerKey,
            WidgetKey = widgetKey,
            Title = "Revenue"
        };

        var expectedData = new ChartDataResponse
        {
            Categories = new List<string> { "Jan", "Feb", "Mar" },
            Series = new List<ChartSeriesDto>
            {
                new ChartSeriesDto
                {
                    Name = "Revenue",
                    Data = new List<decimal> { 1000, 2000, 3000 }
                }
            }
        };

        _mockRegistry.Setup(x => x.Find(providerKey, widgetKey))
            .Returns(definition);

        _mockRegistry.Setup(x => x.QueryAsync(providerKey, widgetKey, query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _controller.Query(providerKey, widgetKey, query, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value as ChartDataResponse;
        data.Should().NotBeNull();
        data!.Categories.Should().HaveCount(3);
        data.Series.Should().HaveCount(1);
    }

    [Fact]
    public async Task Query_WithNonExistingWidget_ReturnsNotFound()
    {
        // Arrange
        var providerKey = "sales";
        var widgetKey = "nonexisting";
        var query = new DashboardQuery();

        _mockRegistry.Setup(x => x.Find(providerKey, widgetKey))
            .Returns((DashboardWidgetDefinition?)null);

        // Act
        var result = await _controller.Query(providerKey, widgetKey, query, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Query_WithEmptyData_ReturnsEmptyDataset()
    {
        // Arrange
        var providerKey = "sales";
        var widgetKey = "revenue";
        var query = new DashboardQuery();

        var definition = new DashboardWidgetDefinition
        {
            ProviderKey = providerKey,
            WidgetKey = widgetKey,
            Title = "Revenue"
        };

        var emptyData = new ChartDataResponse
        {
            Categories = new List<string>(),
            Series = new List<ChartSeriesDto>()
        };

        _mockRegistry.Setup(x => x.Find(providerKey, widgetKey))
            .Returns(definition);

        _mockRegistry.Setup(x => x.QueryAsync(providerKey, widgetKey, query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyData);

        // Act
        var result = await _controller.Query(providerKey, widgetKey, query, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value as ChartDataResponse;
        data.Should().NotBeNull();
        data!.Categories.Should().BeEmpty();
        data.Series.Should().BeEmpty();
    }

    #endregion
}
