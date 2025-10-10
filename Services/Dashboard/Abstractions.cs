using erp.DTOs.Dashboard;

namespace erp.Services.Dashboard;

/// <summary>
/// Abstração para provedores de widgets do dashboard. Cada módulo (ex.: Vendas) registra um provider
/// que anuncia seus widgets e atende consultas de dados para cada widget.
/// </summary>
public interface IDashboardWidgetProvider
{
    string ProviderKey { get; }
    IEnumerable<DashboardWidgetDefinition> GetWidgets();
    Task<ChartDataResponse> QueryAsync(string widgetKey, DashboardQuery query, CancellationToken ct = default);
}

/// <summary>
/// Registro/roteador dos providers. Centraliza listagem e delega queries para o provider correto.
/// </summary>
public interface IDashboardRegistry
{
    IEnumerable<DashboardWidgetDefinition> ListAll();
    IEnumerable<DashboardWidgetDefinition> ListByProvider(string providerKey);
    DashboardWidgetDefinition? Find(string providerKey, string widgetKey);
    Task<ChartDataResponse> QueryAsync(string providerKey, string widgetKey, DashboardQuery query, CancellationToken ct = default);
}

public class DashboardRegistry : IDashboardRegistry
{
    private readonly IReadOnlyDictionary<string, IDashboardWidgetProvider> _providers;

    public DashboardRegistry(IEnumerable<IDashboardWidgetProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.ProviderKey, StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<DashboardWidgetDefinition> ListAll() => _providers.Values.SelectMany(p => p.GetWidgets());

    public IEnumerable<DashboardWidgetDefinition> ListByProvider(string providerKey)
        => _providers.TryGetValue(providerKey, out var p)
            ? p.GetWidgets()
            : Enumerable.Empty<DashboardWidgetDefinition>();

    public DashboardWidgetDefinition? Find(string providerKey, string widgetKey)
        => ListByProvider(providerKey).FirstOrDefault(w => string.Equals(w.WidgetKey, widgetKey, StringComparison.OrdinalIgnoreCase));

    public Task<ChartDataResponse> QueryAsync(string providerKey, string widgetKey, DashboardQuery query, CancellationToken ct = default)
    {
        if (!_providers.TryGetValue(providerKey, out var provider))
        {
            throw new KeyNotFoundException($"Dashboard provider '{providerKey}' not found.");
        }
        return provider.QueryAsync(widgetKey, query, ct);
    }
}
