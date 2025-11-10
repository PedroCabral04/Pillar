using erp.DTOs.Dashboard;

namespace erp.Services.Dashboard.Providers.Finance;

public class FinanceDashboardProvider : IDashboardWidgetProvider
{
    public const string Key = "finance";
    public string ProviderKey => Key;

    public IEnumerable<DashboardWidgetDefinition> GetWidgets() => new[]
    {
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "cashflow",
            Title = "Fluxo de Caixa",
            Description = "Entradas vs Saídas",
            ChartType = DashboardChartType.Area,
            Icon = "mdi-cash",
            Unit = "R$"
            , RequiredRoles = new[] { "Financeiro", "Administrador" }
        }
    };

    public Task<ChartDataResponse> QueryAsync(string widgetKey, DashboardQuery query, CancellationToken ct = default)
    {
        return widgetKey switch
        {
            "cashflow" => Task.FromResult(MockCashflow(query)),
            _ => throw new KeyNotFoundException($"Widget '{widgetKey}' not found in provider '{Key}'.")
        };
    }

    private static ChartDataResponse MockCashflow(DashboardQuery q)
    {
        var months = Enumerable.Range(0, 6)
            .Select(i => DateTime.Now.AddMonths(-5 + i))
            .Select(d => d.ToString("MMM/yy"))
            .ToList();
        var rnd = new Random(7);
        var entradas = new ChartSeriesDto { Name = "Entradas", Data = months.Select(_ => (decimal)rnd.Next(80, 200)).ToList() };
        var saidas = new ChartSeriesDto { Name = "Saídas", Data = months.Select(_ => (decimal)rnd.Next(40, 140)).ToList() };
        return new ChartDataResponse
        {
            Categories = months,
            Series = new List<ChartSeriesDto> { entradas, saidas },
            Subtitle = "Últimos 6 meses"
        };
    }
}
