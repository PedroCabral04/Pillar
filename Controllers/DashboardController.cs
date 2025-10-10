using erp.DTOs.Dashboard;
using erp.Services.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace erp.Controllers;

[ApiController]
[Route("api/dashboard")] 
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardRegistry _registry;

    public DashboardController(IDashboardRegistry registry)
    {
        _registry = registry;
    }

    [HttpGet("widgets")]
    public ActionResult<IEnumerable<DashboardWidgetDefinition>> GetWidgets()
        => Ok(_registry.ListAll());

    [HttpGet("widgets/{providerKey}")]
    public ActionResult<IEnumerable<DashboardWidgetDefinition>> GetWidgetsByProvider(string providerKey)
        => Ok(_registry.ListByProvider(providerKey));

    [HttpPost("query/{providerKey}/{widgetKey}")]
    public async Task<ActionResult<ChartDataResponse>> Query(string providerKey, string widgetKey, [FromBody] DashboardQuery query, CancellationToken ct)
    {
        var def = _registry.Find(providerKey, widgetKey);
        if (def is null) return NotFound();
        var data = await _registry.QueryAsync(providerKey, widgetKey, query, ct);
        return Ok(data);
    }
}
