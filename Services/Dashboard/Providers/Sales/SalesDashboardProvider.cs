using Microsoft.EntityFrameworkCore;
using erp.Data;
using erp.DTOs.Dashboard;
using erp.DTOs.Reports;
using erp.Services.Reports;

namespace erp.Services.Dashboard.Providers.Sales;

public class SalesDashboardProvider : IDashboardWidgetProvider
{
    private readonly ApplicationDbContext _context;
    private readonly ISalesReportService _salesReportService;
    public const string Key = "sales";
    public string ProviderKey => Key;

    public SalesDashboardProvider(ApplicationDbContext context, ISalesReportService salesReportService)
    {
        _context = context;
        _salesReportService = salesReportService;
    }

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
            , RequiredRoles = new[] { "Vendas", "Administrador" }
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "top-products",
            Title = "Top Produtos Vendidos",
            Description = "Produtos mais vendidos",
            ChartType = DashboardChartType.Pie,
            Icon = "mdi-star",
            Unit = "unidades"
            , RequiredRoles = new[] { "Vendas", "Estoque", "Administrador" }
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "sales-by-status",
            Title = "Vendas por Status",
            Description = "Distribuição de vendas por status",
            ChartType = DashboardChartType.Donut,
            Icon = "mdi-chart-donut",
            Unit = "vendas"
            , RequiredRoles = new[] { "Vendas", "Administrador" }
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "sales-peak-hours",
            Title = "Horários de Pico de Vendas",
            Description = "Análise de horários com maior volume de vendas",
            ChartType = DashboardChartType.Bar,
            Icon = "mdi-clock-time-eight",
            Unit = "vendas"
            , RequiredRoles = new[] { "Vendas", "Gerente", "Administrador" }
        }
    };

    public Task<ChartDataResponse> QueryAsync(string widgetKey, DashboardQuery query, CancellationToken ct = default)
    {
        return widgetKey switch
        {
            "sales-by-month" => GetSalesByMonthAsync(query, ct),
            "top-products" => GetTopProductsAsync(query, ct),
            "sales-by-status" => GetSalesByStatusAsync(query, ct),
            "sales-peak-hours" => GetSalesPeakHoursAsync(query, ct),
            _ => throw new KeyNotFoundException($"Widget '{widgetKey}' not found in provider '{Key}'.")
        };
    }

    private async Task<ChartDataResponse> GetSalesByMonthAsync(DashboardQuery query, CancellationToken ct)
    {
        var startDate = (query.From ?? DateTime.Now.AddMonths(-11).Date).ToUniversalTime();
        var endDate = (query.To ?? DateTime.Now.Date).ToUniversalTime();

        var salesByMonth = await _context.Sales
            .Where(s => s.Status == "Finalizada" && 
                       s.SaleDate >= startDate && 
                       s.SaleDate <= endDate)
            .GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Total = g.Sum(s => s.NetAmount)
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync(ct);

        var months = new List<string>();
        var current = new DateTime(startDate.Year, startDate.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        while (current <= endDate)
        {
            months.Add(current.ToString("MMM/yy"));
            current = current.AddMonths(1);
        }

        var salesDict = salesByMonth.ToDictionary(
            s => $"{s.Month}/{s.Year}",
            s => s.Total
        );

        var data = months.Select(m =>
        {
            var parts = m.Split('/');
            var monthNum = DateTime.ParseExact(parts[0], "MMM", System.Globalization.CultureInfo.CurrentCulture).Month;
            var year = int.Parse("20" + parts[1]);
            var key = $"{monthNum}/{year}";
            return salesDict.GetValueOrDefault(key, 0);
        }).ToList();

        return new ChartDataResponse
        {
            Categories = months,
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Receita", Data = data }
            },
            Subtitle = $"Total: {data.Sum(d => d):C2}"
        };
    }

    private async Task<ChartDataResponse> GetTopProductsAsync(DashboardQuery query, CancellationToken ct)
    {
        var startDate = (query.From ?? DateTime.Now.AddMonths(-1).Date).ToUniversalTime();
        var endDate = (query.To ?? DateTime.Now.Date).ToUniversalTime();

        var topProducts = await _context.SaleItems
            .Include(i => i.Sale)
            .Include(i => i.Product)
            .Where(i => i.Sale.Status == "Finalizada" &&
                       i.Sale.SaleDate >= startDate &&
                       i.Sale.SaleDate <= endDate)
            .GroupBy(i => new { i.Product.Name })
            .Select(g => new
            {
                ProductName = g.Key.Name,
                Quantity = g.Sum(i => i.Quantity)
            })
            .OrderByDescending(x => x.Quantity)
            .Take(10)
            .ToListAsync(ct);

        if (!topProducts.Any())
        {
            return new ChartDataResponse
            {
                Categories = new List<string> { "Sem dados" },
                Series = new List<ChartSeriesDto> { new() { Name = "Quantidade", Data = new List<decimal> { 0 } } },
                Subtitle = "Nenhuma venda no período"
            };
        }

        return new ChartDataResponse
        {
            Categories = topProducts.Select(p => p.ProductName).ToList(),
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Quantidade", Data = topProducts.Select(p => (decimal)p.Quantity).ToList() }
            },
            Subtitle = $"Top {topProducts.Count} produtos"
        };
    }

    private async Task<ChartDataResponse> GetSalesByStatusAsync(DashboardQuery query, CancellationToken ct)
    {
        var startDate = (query.From ?? DateTime.Now.AddMonths(-1).Date).ToUniversalTime();
        var endDate = (query.To ?? DateTime.Now.Date).ToUniversalTime();

        var salesByStatus = await _context.Sales
            .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate)
            .GroupBy(s => s.Status)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count()
            })
            .ToListAsync(ct);

        if (!salesByStatus.Any())
        {
            return new ChartDataResponse
            {
                Categories = new List<string> { "Sem dados" },
                Series = new List<ChartSeriesDto> { new() { Name = "Vendas", Data = new List<decimal> { 0 } } },
                Subtitle = "Nenhuma venda no período"
            };
        }

        return new ChartDataResponse
        {
            Categories = salesByStatus.Select(s => s.Status).ToList(),
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Vendas", Data = salesByStatus.Select(s => (decimal)s.Count).ToList() }
            },
            Subtitle = $"Total: {salesByStatus.Sum(s => s.Count)} vendas"
        };
    }

    private async Task<ChartDataResponse> GetSalesPeakHoursAsync(DashboardQuery query, CancellationToken ct)
    {
        var filter = new SalesReportFilterDto
        {
            StartDate = query.From ?? DateTime.Now.AddMonths(-1),
            EndDate = query.To ?? DateTime.Now
        };

        var heatmapReport = await _salesReportService.GenerateSalesHeatmapAsync(filter);
        var summary = heatmapReport.Summary;

        if (heatmapReport.Series.Count == 0)
        {
            return new ChartDataResponse
            {
                Categories = new List<string> { "Sem dados" },
                Series = new List<ChartSeriesDto> { new() { Name = "Vendas", Data = new List<decimal> { 0 } } },
                Subtitle = "Nenhuma venda no período"
            };
        }

        // Aggregate sales by hour (summing all days)
        var hourlyTotals = new Dictionary<string, int>();
        
        foreach (var daySeries in heatmapReport.Series)
        {
            foreach (var dataPoint in daySeries.Data)
            {
                if (!hourlyTotals.ContainsKey(dataPoint.X))
                    hourlyTotals[dataPoint.X] = 0;
                hourlyTotals[dataPoint.X] += dataPoint.Y;
            }
        }

        // Get top 12 hours for readability, sorted by hour
        var topHours = hourlyTotals
            .OrderByDescending(kv => kv.Value)
            .Take(12)
            .OrderBy(kv => kv.Key)
            .ToList();

        var categories = topHours.Select(h => h.Key).ToList();
        var values = topHours.Select(h => (decimal)h.Value).ToList();

        // Find peak info
        var peakHourEntry = hourlyTotals.OrderByDescending(kv => kv.Value).FirstOrDefault();
        var peakDay = summary.PeakDay;
        var peakHour = peakHourEntry.Key ?? "N/A";
        var peakCount = peakHourEntry.Value;

        return new ChartDataResponse
        {
            Categories = categories,
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Vendas", Data = values }
            },
            Subtitle = $"Pico: {peakHour} ({peakCount:N0} vendas) | Dia mais movimentado: {peakDay}"
        };
    }
}
