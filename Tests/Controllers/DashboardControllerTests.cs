using System.Security.Claims;
using erp.Controllers;
using erp.DTOs.Dashboard;
using erp.Services.Dashboard;
using erp.Services.DashboardCustomization;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace erp.Tests.Controllers;

public class DashboardControllerTests
{
    private readonly Mock<IDashboardRegistry> _registry;
    private readonly Mock<IDashboardLayoutService> _layoutService;
    private readonly DashboardController _controller;

    public DashboardControllerTests()
    {
        _registry = new Mock<IDashboardRegistry>();
        _layoutService = new Mock<IDashboardLayoutService>();
        _controller = new DashboardController(_registry.Object, _layoutService.Object);

        SetUserRoles("Vendas");
    }

    [Fact]
    public void GetWidgets_FiltersByRolesAndReturnsCatalog()
    {
        _layoutService.Setup(x => x.GetAvailableWidgets(It.IsAny<string[]>()))
            .Returns(new List<WidgetCatalogItem>
            {
                new() { ProviderKey = "sales", WidgetKey = "public", Title = "Publico", Description = "Desc", RequiredRoles = null },
                new() { ProviderKey = "sales", WidgetKey = "sales-only", Title = "Vendas", Description = "Desc", RequiredRoles = ["Vendas"] },
                new() { ProviderKey = "finance", WidgetKey = "finance-only", Title = "Financeiro", Description = "Desc", RequiredRoles = ["Financeiro"] }
            });

        _registry.Setup(x => x.Find("sales", "public"))
            .Returns(new DashboardWidgetDefinition { ProviderKey = "sales", WidgetKey = "public", Title = "Publico", Description = "Desc", ChartType = DashboardChartType.Bar });
        _registry.Setup(x => x.Find("sales", "sales-only"))
            .Returns(new DashboardWidgetDefinition { ProviderKey = "sales", WidgetKey = "sales-only", Title = "Vendas", Description = "Desc", ChartType = DashboardChartType.Line });

        var result = _controller.GetWidgets();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var widgets = ok.Value.Should().BeAssignableTo<IEnumerable<DashboardWidgetDefinition>>().Subject.ToList();
        widgets.Select(w => w.WidgetKey).Should().Contain(["public", "sales-only"]);
        widgets.Select(w => w.WidgetKey).Should().NotContain("finance-only");
    }

    [Fact]
    public void GetWidgetsByProvider_ReturnsOnlyMatchingProvider()
    {
        _layoutService.Setup(x => x.GetAvailableWidgets(It.IsAny<string[]>()))
            .Returns(new List<WidgetCatalogItem>
            {
                new() { ProviderKey = "sales", WidgetKey = "a", Title = "A", Description = "Desc" },
                new() { ProviderKey = "inventory", WidgetKey = "b", Title = "B", Description = "Desc" }
            });

        _registry.Setup(x => x.Find("sales", "a"))
            .Returns(new DashboardWidgetDefinition { ProviderKey = "sales", WidgetKey = "a", Title = "A", Description = "Desc", ChartType = DashboardChartType.Bar });

        var result = _controller.GetWidgetsByProvider("sales");

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var widgets = ok.Value.Should().BeAssignableTo<IEnumerable<DashboardWidgetDefinition>>().Subject.ToList();
        widgets.Should().HaveCount(1);
        widgets[0].ProviderKey.Should().Be("sales");
    }

    [Fact]
    public async Task Query_WhenWidgetMissing_ReturnsNotFound()
    {
        _registry.Setup(x => x.Find("sales", "missing")).Returns((DashboardWidgetDefinition?)null);

        var result = await _controller.Query("sales", "missing", new DashboardQuery(), CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Query_WhenRoleDoesNotMatch_ReturnsForbid()
    {
        _registry.Setup(x => x.Find("sales", "revenue"))
            .Returns(new DashboardWidgetDefinition { ProviderKey = "sales", WidgetKey = "revenue", Title = "Receita", Description = "Desc" });
        _layoutService.Setup(x => x.GetWidgetRolesAsync("sales", "revenue"))
            .ReturnsAsync(["Financeiro"]);

        var result = await _controller.Query("sales", "revenue", new DashboardQuery(), CancellationToken.None);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    private void SetUserRoles(params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "1") };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
            }
        };
    }
}
