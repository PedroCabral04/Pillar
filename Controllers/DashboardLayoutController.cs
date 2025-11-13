using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using erp.Services.DashboardCustomization;
using erp.DTOs.Dashboard;
using System.Security.Claims;

namespace erp.Controllers;

[ApiController]
[Route("api/dashboard/[controller]")]
[Authorize]
public class LayoutController : ControllerBase
{
    private readonly IDashboardLayoutService _layoutService;

    public LayoutController(IDashboardLayoutService layoutService)
    {
        _layoutService = layoutService;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardLayout>> GetLayout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var layout = await _layoutService.GetUserLayoutAsync(userId);
        return Ok(layout);
    }

    [HttpPost]
    public async Task<ActionResult> SaveLayout([FromBody] SaveLayoutRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var layout = new DashboardLayout
        {
            UserId = userId,
            Widgets = request.Widgets,
            LayoutType = request.LayoutType,
            Columns = request.Columns
        };

        await _layoutService.SaveUserLayoutAsync(userId, layout);
        return Ok(new { message = "Layout saved successfully" });
    }

    [HttpPost("reset")]
    public async Task<ActionResult> ResetLayout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await _layoutService.ResetToDefaultAsync(userId);
        return Ok(new { message = "Layout reset to default" });
    }

    [HttpGet("catalog")]
    public ActionResult<List<WidgetCatalogItem>> GetWidgetCatalog()
    {
        var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        var catalog = _layoutService.GetAvailableWidgets(userRoles);
        return Ok(catalog);
    }

    [HttpGet("widgets/{providerKey}/{widgetKey}/roles")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<string[]?>> GetWidgetRoles(string providerKey, string widgetKey)
    {
        var roles = await _layoutService.GetWidgetRolesAsync(providerKey, widgetKey);
        return Ok(roles ?? Array.Empty<string>());
    }

    [HttpPost("widgets/{providerKey}/{widgetKey}/roles")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult> SetWidgetRoles(string providerKey, string widgetKey, [FromBody] string[]? roles)
    {
        await _layoutService.SetWidgetRolesAsync(providerKey, widgetKey, roles);
        return Ok(new { message = "Widget roles updated" });
    }

    [HttpPost("widgets")]
    public async Task<ActionResult<WidgetConfiguration>> AddWidget([FromBody] AddWidgetRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var widget = await _layoutService.AddWidgetAsync(
            userId, 
            request.ProviderKey, 
            request.WidgetKey,
            request.Row,
            request.Column
        );

        if (widget == null)
        {
            return BadRequest(new { error = "Widget already exists or could not be added" });
        }

        return Ok(widget);
    }

    [HttpDelete("widgets/{widgetId}")]
    public async Task<ActionResult> RemoveWidget(string widgetId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var success = await _layoutService.RemoveWidgetAsync(userId, widgetId);
        if (!success)
        {
            return NotFound(new { error = "Widget not found" });
        }

        return Ok(new { message = "Widget removed successfully" });
    }

    [HttpPatch("widgets/{widgetId}")]
    public async Task<ActionResult> UpdateWidget(string widgetId, [FromBody] UpdateWidgetRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var success = await _layoutService.UpdateWidgetAsync(userId, widgetId, request);
        if (!success)
        {
            return NotFound(new { error = "Widget not found" });
        }

        return Ok(new { message = "Widget updated successfully" });
    }

    [HttpPost("widgets/reorder")]
    public async Task<ActionResult> ReorderWidgets([FromBody] List<string> widgetOrder)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var success = await _layoutService.ReorderWidgetsAsync(userId, widgetOrder);
        if (!success)
        {
            return BadRequest(new { error = "Failed to reorder widgets" });
        }

        return Ok(new { message = "Widgets reordered successfully" });
    }
}
