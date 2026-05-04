using erp.DTOs.Dashboard;
using erp.Models.Identity;
using erp.Services.Authorization;
using erp.Services.Dashboard;
using erp.Services.Dashboard.Providers.Sales;
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
    private readonly IPermissionService _permissionService;

    public DashboardController(IDashboardRegistry registry, IDashboardLayoutService layoutService, IPermissionService permissionService)
    {
        _registry = registry;
        _layoutService = layoutService;
        _permissionService = permissionService;
    }

    private async Task<bool> CanAccessSalesWidgetsAsync()
    {
        return await _permissionService.HasModuleActionAccessAsync(User, ModuleKeys.Sales, ModuleActionKeys.Sales.ViewValues);
    }

    /// <summary>
    /// Retorna as definições de widgets do painel disponíveis para o usuário atual.
    /// Os widgets são filtrados pelos papéis (roles) do usuário e por quaisquer sobrescritas de papéis armazenadas no serviço de layout.
    /// </summary>
    /// <returns>200 OK com uma coleção de <see cref="DashboardWidgetDefinition"/>.</returns>
    [HttpGet("widgets")]
    public async Task<ActionResult<IEnumerable<DashboardWidgetDefinition>>> GetWidgets()
    {
        var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        var canViewSalesValues = await CanAccessSalesWidgetsAsync();
        
        // Get widgets with role overrides from database
        var catalog = _layoutService.GetAvailableWidgets(userRoles);
        
        // Filter by user roles - if no roles required, show to everyone; if roles required, user must have at least one
        var filteredWidgets = catalog
            .Where(w => w.RequiredRoles == null || w.RequiredRoles.Length == 0 || w.RequiredRoles.Intersect(userRoles).Any())
            .Where(w => canViewSalesValues || w.ProviderKey != SalesDashboardProvider.Key)
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

    /// <summary>
    /// Retorna as definições de widgets para um provedor específico, filtradas pelos papéis do usuário atual.
    /// </summary>
    /// <param name="providerKey">A chave do provedor para filtrar os widgets.</param>
    /// <returns>200 OK com uma coleção de <see cref="DashboardWidgetDefinition"/> para o provedor.</returns>
    [HttpGet("widgets/{providerKey}")]
    public async Task<ActionResult<IEnumerable<DashboardWidgetDefinition>>> GetWidgetsByProvider(string providerKey)
    {
        var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        var catalog = _layoutService.GetAvailableWidgets(userRoles);
        
        if (providerKey == SalesDashboardProvider.Key && !await CanAccessSalesWidgetsAsync())
        {
            return Ok(Enumerable.Empty<DashboardWidgetDefinition>());
        }
        
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

    /// <summary>
    /// Executa a query registrada do widget e retorna os dados do gráfico para o provedor/widget informados.
    /// Valida se a definição do widget existe e se o usuário atual possui os papéis necessários antes de executar.
    /// </summary>
    /// <param name="providerKey">A chave do provedor do widget.</param>
    /// <param name="widgetKey">A chave do widget para executar a query.</param>
    /// <param name="query">O payload da query contendo filtros e intervalos de tempo.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>
    /// 200 OK com <see cref="ChartDataResponse"/> em caso de sucesso, 404 se a definição do widget não for encontrada,
    /// ou 403 se o usuário não tiver acesso ao widget.
    /// </returns>
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

        // Block sales widgets if user doesn't have view_values permission
        if (providerKey == SalesDashboardProvider.Key && !await CanAccessSalesWidgetsAsync())
        {
            return Forbid();
        }
        
        var data = await _registry.QueryAsync(providerKey, widgetKey, query, ct);
        return Ok(data);
    }
}
