using erp.DTOs.Dashboard;

namespace erp.Services.Dashboard;

public interface IDashboardService
{
    Task<IReadOnlyList<DashboardWidgetDefinition>> GetWidgetsAsync(CancellationToken ct = default);
    Task<ChartDataResponse?> QueryAsync(DashboardWidgetDefinition def, DashboardQuery query, CancellationToken ct = default);
}

public class DashboardService : IDashboardService
{
    private readonly IApiService _api;
    private IReadOnlyList<DashboardWidgetDefinition>? _cache;

    public DashboardService(IApiService api)
    {
        _api = api;
    }

    public async Task<IReadOnlyList<DashboardWidgetDefinition>> GetWidgetsAsync(CancellationToken ct = default)
    {
        if (_cache is not null) return _cache;
        var result = await _api.GetAsync<List<DashboardWidgetDefinition>>("api/dashboard/widgets", ct);
        _cache = result ?? new List<DashboardWidgetDefinition>();
        return _cache;
    }

    public async Task<ChartDataResponse?> QueryAsync(DashboardWidgetDefinition def, DashboardQuery query, CancellationToken ct = default)
    {
        using var resp = await _api.PostAsJsonAsync($"api/dashboard/query/{def.ProviderKey}/{def.WidgetKey}", query, ct);
        if (!resp.IsSuccessStatusCode) return null;
        var json = await resp.Content.ReadAsStringAsync(ct);
        return System.Text.Json.JsonSerializer.Deserialize<ChartDataResponse>(json, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}
