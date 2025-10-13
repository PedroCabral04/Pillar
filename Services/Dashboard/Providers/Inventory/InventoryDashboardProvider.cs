using Microsoft.EntityFrameworkCore;
using erp.Data;
using erp.DTOs.Dashboard;

namespace erp.Services.Dashboard.Providers.Inventory;

public class InventoryDashboardProvider : IDashboardWidgetProvider
{
    private readonly ApplicationDbContext _context;
    public const string Key = "inventory";
    public string ProviderKey => Key;

    public InventoryDashboardProvider(ApplicationDbContext context)
    {
        _context = context;
    }

    public IEnumerable<DashboardWidgetDefinition> GetWidgets() => new[]
    {
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "stock-levels",
            Title = "Níveis de Estoque",
            Description = "Distribuição de produtos por nível de estoque",
            ChartType = DashboardChartType.Pie,
            Icon = "mdi-package-variant",
            Unit = "produtos"
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "low-stock-alert",
            Title = "Alertas de Estoque Baixo",
            Description = "Produtos com estoque abaixo do mínimo",
            ChartType = DashboardChartType.Bar,
            Icon = "mdi-alert",
            Unit = "unidades"
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "stock-movements",
            Title = "Movimentações de Estoque",
            Description = "Entradas e saídas por mês",
            ChartType = DashboardChartType.Line,
            Icon = "mdi-swap-horizontal",
            Unit = "movimentações"
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "top-value-products",
            Title = "Produtos de Maior Valor",
            Description = "Top 10 produtos por valor em estoque",
            ChartType = DashboardChartType.Bar,
            Icon = "mdi-currency-usd",
            Unit = "R$"
        }
    };

    public Task<ChartDataResponse> QueryAsync(string widgetKey, DashboardQuery query, CancellationToken ct = default)
    {
        return widgetKey switch
        {
            "stock-levels" => GetStockLevelsAsync(query, ct),
            "low-stock-alert" => GetLowStockAlertAsync(query, ct),
            "stock-movements" => GetStockMovementsAsync(query, ct),
            "top-value-products" => GetTopValueProductsAsync(query, ct),
            _ => throw new KeyNotFoundException($"Widget '{widgetKey}' not found in provider '{Key}'.")
        };
    }

    private async Task<ChartDataResponse> GetStockLevelsAsync(DashboardQuery query, CancellationToken ct)
    {
        var products = await _context.Products
            .Where(p => p.Status == Models.Inventory.ProductStatus.Active)
            .ToListAsync(ct);

        var noStock = products.Count(p => p.CurrentStock <= 0);
        var lowStock = products.Count(p => p.CurrentStock > 0 && p.CurrentStock <= p.MinimumStock);
        var normalStock = products.Count(p => p.CurrentStock > p.MinimumStock && p.CurrentStock < p.MaximumStock);
        var overStock = products.Count(p => p.CurrentStock >= p.MaximumStock && p.MaximumStock > 0);

        return new ChartDataResponse
        {
            Categories = new List<string> { "Sem Estoque", "Estoque Baixo", "Estoque Normal", "Excesso" },
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Produtos", Data = new List<decimal> { (decimal)noStock, (decimal)lowStock, (decimal)normalStock, (decimal)overStock } }
            },
            Subtitle = $"Total: {products.Count} produtos"
        };
    }

    private async Task<ChartDataResponse> GetLowStockAlertAsync(DashboardQuery query, CancellationToken ct)
    {
        var lowStockProducts = await _context.Products
            .Where(p => p.Status == Models.Inventory.ProductStatus.Active && 
                       p.CurrentStock > 0 && 
                       p.CurrentStock <= p.MinimumStock)
            .OrderBy(p => p.CurrentStock)
            .Take(10)
            .Select(p => new { p.Name, p.CurrentStock, p.MinimumStock })
            .ToListAsync(ct);

        var categories = lowStockProducts.Select(p => p.Name).ToList();
        var currentStock = lowStockProducts.Select(p => (decimal)p.CurrentStock).ToList();
        var minimumStock = lowStockProducts.Select(p => (decimal)p.MinimumStock).ToList();

        return new ChartDataResponse
        {
            Categories = categories,
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Estoque Atual", Data = currentStock },
                new() { Name = "Estoque Mínimo", Data = minimumStock }
            },
            Subtitle = $"{lowStockProducts.Count} produtos com estoque baixo"
        };
    }

    private async Task<ChartDataResponse> GetStockMovementsAsync(DashboardQuery query, CancellationToken ct)
    {
        var startDate = query.From ?? DateTime.Now.AddMonths(-11).Date;
        var endDate = query.To ?? DateTime.Now.Date;

        var movements = await _context.StockMovements
            .Where(m => m.MovementDate >= startDate && m.MovementDate <= endDate)
            .GroupBy(m => new { m.MovementDate.Year, m.MovementDate.Month, m.Type })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Type,
                Count = g.Count()
            })
            .ToListAsync(ct);

        var months = new List<string>();
        var current = new DateTime(startDate.Year, startDate.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        while (current <= endDate)
        {
            months.Add(current.ToString("MMM/yy"));
            current = current.AddMonths(1);
        }

        var entries = movements
            .Where(m => m.Type == Models.Inventory.MovementType.In)
            .GroupBy(m => new { m.Year, m.Month })
            .ToDictionary(g => $"{g.Key.Month}/{g.Key.Year}", g => (decimal)g.Sum(x => x.Count));

        var exits = movements
            .Where(m => m.Type == Models.Inventory.MovementType.Out)
            .GroupBy(m => new { m.Year, m.Month })
            .ToDictionary(g => $"{g.Key.Month}/{g.Key.Year}", g => (decimal)g.Sum(x => x.Count));

        var entriesData = months.Select(m =>
        {
            var parts = m.Split('/');
            var monthNum = DateTime.ParseExact(parts[0], "MMM", System.Globalization.CultureInfo.CurrentCulture).Month;
            var year = int.Parse("20" + parts[1]);
            var key = $"{monthNum}/{year}";
            return entries.GetValueOrDefault(key, 0);
        }).ToList();

        var exitsData = months.Select(m =>
        {
            var parts = m.Split('/');
            var monthNum = DateTime.ParseExact(parts[0], "MMM", System.Globalization.CultureInfo.CurrentCulture).Month;
            var year = int.Parse("20" + parts[1]);
            var key = $"{monthNum}/{year}";
            return exits.GetValueOrDefault(key, 0);
        }).ToList();

        return new ChartDataResponse
        {
            Categories = months,
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Entradas", Data = entriesData },
                new() { Name = "Saídas", Data = exitsData }
            },
            Subtitle = "Últimos 12 meses"
        };
    }

    private async Task<ChartDataResponse> GetTopValueProductsAsync(DashboardQuery query, CancellationToken ct)
    {
        var topProducts = await _context.Products
            .Where(p => p.Status == Models.Inventory.ProductStatus.Active && p.CurrentStock > 0)
            .OrderByDescending(p => p.CurrentStock * p.CostPrice)
            .Take(10)
            .Select(p => new
            {
                p.Name,
                Value = p.CurrentStock * p.CostPrice
            })
            .ToListAsync(ct);

        return new ChartDataResponse
        {
            Categories = topProducts.Select(p => p.Name).ToList(),
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Valor em Estoque", Data = topProducts.Select(p => p.Value).ToList() }
            },
            Subtitle = "Top 10 produtos"
        };
    }
}
