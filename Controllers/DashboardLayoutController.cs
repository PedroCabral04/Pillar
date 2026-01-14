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

    /// <summary>
    /// Retorna o layout do dashboard do usuário autenticado.
    /// </summary>
    /// <returns>Objeto <see cref="DashboardLayout"/> com a configuração do layout do usuário.</returns>
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

    /// <summary>
    /// Salva o layout do dashboard para o usuário autenticado.
    /// </summary>
    /// <param name="request">Dados do layout a serem salvos.</param>
    /// <returns>Resposta HTTP 200 em caso de sucesso.</returns>
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

    /// <summary>
    /// Restaura o layout do usuário para o padrão do sistema.
    /// </summary>
    /// <returns>Resposta HTTP 200 em caso de sucesso.</returns>
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

    /// <summary>
    /// Retorna o catálogo de widgets disponíveis para o usuário, filtrado por suas roles.
    /// </summary>
    /// <returns>Lista de <see cref="WidgetCatalogItem"/> disponíveis.</returns>
    [HttpGet("catalog")]
    public ActionResult<List<WidgetCatalogItem>> GetWidgetCatalog()
    {
        var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        var catalog = _layoutService.GetAvailableWidgets(userRoles);
        return Ok(catalog);
    }

    /// <summary>
    /// Obtém as roles permitidas para um widget específico. Requer role 'Administrador'.
    /// </summary>
    /// <param name="providerKey">Chave do provedor do widget.</param>
    /// <param name="widgetKey">Chave do widget.</param>
    /// <returns>Array de roles (strings) ou vazio se não houver roles configuradas.</returns>
    [HttpGet("widgets/{providerKey}/{widgetKey}/roles")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<string[]?>> GetWidgetRoles(string providerKey, string widgetKey)
    {
        var roles = await _layoutService.GetWidgetRolesAsync(providerKey, widgetKey);
        return Ok(roles ?? Array.Empty<string>());
    }

    /// <summary>
    /// Define as roles que podem usar o widget especificado. Requer role 'Administrador'.
    /// </summary>
    /// <param name="providerKey">Chave do provedor do widget.</param>
    /// <param name="widgetKey">Chave do widget.</param>
    /// <param name="roles">Array de roles a serem atribuídas (pode ser nulo para limpar).</param>
    /// <returns>Resposta HTTP 200 em caso de sucesso.</returns>
    [HttpPost("widgets/{providerKey}/{widgetKey}/roles")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult> SetWidgetRoles(string providerKey, string widgetKey, [FromBody] string[]? roles)
    {
        await _layoutService.SetWidgetRolesAsync(providerKey, widgetKey, roles);
        return Ok(new { message = "Widget roles updated" });
    }

    /// <summary>
    /// Adiciona um novo widget ao layout do usuário autenticado.
    /// </summary>
    /// <param name="request">Dados do widget a ser adicionado.</param>
    /// <returns>Objeto <see cref="WidgetConfiguration"/> do widget adicionado ou BadRequest se não for possível.</returns>
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

    /// <summary>
    /// Remove um widget do layout do usuário autenticado.
    /// </summary>
    /// <param name="widgetId">Identificador do widget a ser removido.</param>
    /// <returns>Resposta HTTP 200 em caso de sucesso ou 404 se o widget não existir.</returns>
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

    /// <summary>
    /// Atualiza as propriedades de um widget no layout do usuário autenticado.
    /// </summary>
    /// <param name="widgetId">Identificador do widget a ser atualizado.</param>
    /// <param name="request">Propriedades a atualizar.</param>
    /// <returns>Resposta HTTP 200 em caso de sucesso ou 404 se o widget não existir.</returns>
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

    /// <summary>
    /// Reordena a lista de widgets do usuário de acordo com a ordem enviada.
    /// </summary>
    /// <param name="widgetOrder">Lista de IDs de widgets na ordem desejada.</param>
    /// <returns>Resposta HTTP 200 em caso de sucesso.</returns>
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
