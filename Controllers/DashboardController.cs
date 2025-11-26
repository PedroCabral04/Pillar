using erp.DTOs.Dashboard;
using erp.Services.Dashboard;
using erp.Services.DashboardCustomization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace erp.Controllers;

[ApiController]
[Route("api/dashboard")] 
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardRegistry _registry;
    private readonly IDashboardLayoutService _layoutService;

    public DashboardController(IDashboardRegistry registry, IDashboardLayoutService layoutService)
    {
        _registry = registry;
        _layoutService = layoutService;
    }

    [HttpGet("widgets")]
    public ActionResult<IEnumerable<DashboardWidgetDefinition>> GetWidgets()
    {
        var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        
        // Get widgets with role overrides from database
        var catalog = _layoutService.GetAvailableWidgets(userRoles);
        
        // Filter by user roles - if no roles required, show to everyone; if roles required, user must have at least one
        var filteredWidgets = catalog
            .Where(w => w.RequiredRoles == null || w.RequiredRoles.Length == 0 || w.RequiredRoles.Intersect(userRoles).Any())
            .Select(w => new DashboardWidgetDefinition
            {
                ProviderKey = w.ProviderKey,
                WidgetKey = w.WidgetKey,
                Title = w.Title,
                Description = w.Description,
                Icon = w.Icon,
                ChartType = _registry.Find(w.ProviderKey, w.WidgetKey)?.ChartType ?? DashboardChartType.Bar,
                Unit = _registry.Find(w.ProviderKey, w.WidgetKey)?.Unit,
                RequiredRoles = w.RequiredRoles
            })
            .ToList();
        
        return Ok(filteredWidgets);
    }

    [HttpGet("widgets/{providerKey}")]
    public ActionResult<IEnumerable<DashboardWidgetDefinition>> GetWidgetsByProvider(string providerKey)
    {
        var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        var catalog = _layoutService.GetAvailableWidgets(userRoles);
        
        var filteredWidgets = catalog
            .Where(w => w.ProviderKey == providerKey)
            .Where(w => w.RequiredRoles == null || w.RequiredRoles.Length == 0 || w.RequiredRoles.Intersect(userRoles).Any())
            .Select(w => new DashboardWidgetDefinition
            {
                ProviderKey = w.ProviderKey,
                WidgetKey = w.WidgetKey,
                Title = w.Title,
                Description = w.Description,
                Icon = w.Icon,
                ChartType = _registry.Find(w.ProviderKey, w.WidgetKey)?.ChartType ?? DashboardChartType.Bar,
                Unit = _registry.Find(w.ProviderKey, w.WidgetKey)?.Unit,
                RequiredRoles = w.RequiredRoles
            });
        
        return Ok(filteredWidgets);
    }

    [HttpPost("query/{providerKey}/{widgetKey}")]
    public async Task<ActionResult<ChartDataResponse>> Query(string providerKey, string widgetKey, [FromBody] DashboardQuery query, CancellationToken ct)
    {
        var def = _registry.Find(providerKey, widgetKey);
        if (def is null) return NotFound();
        
        // Check role access before querying
        var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        var widgetRoles = await _layoutService.GetWidgetRolesAsync(providerKey, widgetKey);
        
        if (widgetRoles != null && widgetRoles.Length > 0 && !widgetRoles.Intersect(userRoles).Any())
        {
            return Forbid();
        }
        
        var data = await _registry.QueryAsync(providerKey, widgetKey, query, ct);
        return Ok(data);
    }
}
