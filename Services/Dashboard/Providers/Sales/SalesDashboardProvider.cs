using erp.DTOs.Dashboard;

namespace erp.Services.Dashboard.Providers.Sales;

public class SalesDashboardProvider : IDashboardWidgetProvider
{
    public const string Key = "sales";
    public string ProviderKey => Key;

    public IEnumerable<DashboardWidgetDefinition> GetWidgets() => new[]
    {
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "sales-by-month",
            Title = "Vendas por Mês",
            Description = "Total de vendas agregadas por mês",
            ChartType = DashboardChartType.Bar,
            Icon = "mdi-chart-bar",
            Unit = "R$"
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "top-products",
            Title = "Top Produtos",
            Description = "Itens mais vendidos",
            ChartType = DashboardChartType.Pie,
            Icon = "mdi-star",
            Unit = null
        }
    };

    // TODO: Replace with real EF Core queries when Sales module exists
    public Task<ChartDataResponse> QueryAsync(string widgetKey, DashboardQuery query, CancellationToken ct = default)
    {
        return widgetKey switch
        {
            "sales-by-month" => Task.FromResult(MockSalesByMonth(query)),
            "top-products" => Task.FromResult(MockTopProducts(query)),
            _ => throw new KeyNotFoundException($"Widget '{widgetKey}' not found in provider '{Key}'.")
        };
    }

    private static ChartDataResponse MockSalesByMonth(DashboardQuery q)
    {
        var months = Enumerable.Range(0, 12)
            .Select(i => DateTime.Now.AddMonths(-11 + i))
            .Select(d => d.ToString("MMM/yy"))
            .ToList();
        var rnd = new Random(42);
        var serie = new ChartSeriesDto { Name = "Receita", Data = months.Select(_ => (decimal)rnd.Next(30, 180)).ToList() };
        return new ChartDataResponse
        {
            Categories = months,
            Series = new List<ChartSeriesDto> { serie },
            Subtitle = "Últimos 12 meses"
        };
    }

    private static ChartDataResponse MockTopProducts(DashboardQuery q)
    {
        var labels = new List<string> { "Produto A", "Produto B", "Produto C", "Produto D" };
        var data = new List<decimal> { 120, 90, 70, 55 };
        return new ChartDataResponse
        {
            Categories = labels,
            Series = new List<ChartSeriesDto> { new() { Name = "Qtd", Data = data } },
            Subtitle = "Top 4"
        };
    }
}
