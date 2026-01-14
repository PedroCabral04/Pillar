using Microsoft.EntityFrameworkCore;
using erp.Data;
using erp.DTOs.Dashboard;
using erp.DTOs.Reports;
using erp.Services.Reports;

namespace erp.Services.Dashboard.Providers.Inventory;

public class InventoryDashboardProvider : IDashboardWidgetProvider
{
    private readonly ApplicationDbContext _context;
    private readonly IInventoryReportService _inventoryReportService;
    public const string Key = "inventory";
    public string ProviderKey => Key;

    public InventoryDashboardProvider(ApplicationDbContext context, IInventoryReportService inventoryReportService)
    {
        _context = context;
        _inventoryReportService = inventoryReportService;
    }

    public IEnumerable<DashboardWidgetDefinition> GetWidgets() => new[]
    {
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "stock-levels",
            Title = "Níveis de Estoque",
            Description = "Distribuição atual de produtos por nível de estoque",
            ChartType = DashboardChartType.Pie,
            Icon = "mdi-package-variant",
            Unit = "produtos",
            RequiredRoles = new[] { "Estoque", "Administrador" }
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "low-stock-alert",
            Title = "Alertas de Estoque Baixo",
            Description = "Produtos com estoque atual abaixo do mínimo",
            ChartType = DashboardChartType.Bar,
            Icon = "mdi-alert",
            Unit = "unidades",
            RequiredRoles = new[] { "Estoque", "Compras", "Administrador" }
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "stock-movements",
            Title = "Movimentações de Estoque",
            Description = "Entradas e saídas no período selecionado",
            ChartType = DashboardChartType.Line,
            Icon = "mdi-swap-horizontal",
            Unit = "movimentações",
            RequiredRoles = new[] { "Estoque", "Administrador" }
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "top-value-products",
            Title = "Produtos de Maior Valor",
            Description = "Top 10 produtos por valor atual em estoque",
            ChartType = DashboardChartType.Bar,
            Icon = "mdi-currency-usd",
            Unit = "R$",
            RequiredRoles = new[] { "Estoque", "Financeiro", "Administrador" }
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "abc-curve",
            Title = "Curva ABC (Pareto)",
            Description = "Classificação de produtos por contribuição de receita no período",
            ChartType = DashboardChartType.Donut,
            Icon = "mdi-chart-arc",
            Unit = "R$",
            RequiredRoles = new[] { "Estoque", "Financeiro", "Gerente", "Administrador" }
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
            "abc-curve" => GetABCCurveAsync(query, ct),
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
                new() { Name = "Produtos", Data = new List<decimal> { noStock, lowStock, normalStock, overStock } }
            },
            Subtitle = $"Total: {products.Count} produtos ativos",
            IsCurrentStateWidget = true,
            DynamicDescription = "Snapshot atual da distribuição de estoque"
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

        // Guard clause: return early if no low-stock products
        if (lowStockProducts.Count == 0)
        {
            return new ChartDataResponse
            {
                Categories = new List<string> { "Nenhum alerta" },
                Series = new List<ChartSeriesDto> { new() { Name = "Estoque", Data = new List<decimal> { 0 } } },
                Subtitle = "Todos os produtos estão com estoque adequado",
                IsCurrentStateWidget = true
            };
        }

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
            Subtitle = $"{lowStockProducts.Count} produtos com estoque baixo",
            IsCurrentStateWidget = true,
            DynamicDescription = "Produtos atualmente com estoque abaixo do mínimo"
        };
    }

    private async Task<ChartDataResponse> GetStockMovementsAsync(DashboardQuery query, CancellationToken ct)
    {
        var startDate = query.From?.ToUniversalTime() ?? DateTime.UtcNow.AddMonths(-11).Date;
        var endDate = query.To?.ToUniversalTime() ?? DateTime.UtcNow.Date;
        var periodLabel = DashboardDateUtils.FormatPeriodLabel(query.From, query.To);

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
        
        var totalEntries = entriesData.Sum();
        var totalExits = exitsData.Sum();

        return new ChartDataResponse
        {
            Categories = months,
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Entradas", Data = entriesData },
                new() { Name = "Saídas", Data = exitsData }
            },
            Subtitle = $"Entradas: {totalEntries:N0} | Saídas: {totalExits:N0}",
            PeriodLabel = periodLabel,
            DynamicDescription = $"Movimentações de estoque de {periodLabel}"
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
        
        if (!topProducts.Any())
        {
            return new ChartDataResponse
            {
                Categories = new List<string> { "Sem dados" },
                Series = new List<ChartSeriesDto> { new() { Name = "Valor", Data = new List<decimal> { 0 } } },
                Subtitle = "Nenhum produto com estoque",
                IsCurrentStateWidget = true
            };
        }
        
        var totalValue = topProducts.Sum(p => p.Value);

        return new ChartDataResponse
        {
            Categories = topProducts.Select(p => p.Name).ToList(),
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Valor em Estoque", Data = topProducts.Select(p => p.Value).ToList() }
            },
            Subtitle = $"Top {topProducts.Count} produtos | Total: {CurrencyFormatService.FormatStatic(totalValue)}",
            IsCurrentStateWidget = true,
            DynamicDescription = "Produtos com maior valor atual em estoque"
        };
    }

    private async Task<ChartDataResponse> GetABCCurveAsync(DashboardQuery query, CancellationToken ct)
    {
        var periodLabel = DashboardDateUtils.FormatPeriodLabel(query.From, query.To);
        var filter = new InventoryReportFilterDto
        {
            StartDate = query.From?.ToUniversalTime() ?? DateTime.UtcNow.AddMonths(-6),
            EndDate = query.To?.ToUniversalTime() ?? DateTime.UtcNow
        };

        var abcReport = await _inventoryReportService.GenerateABCCurveReportAsync(filter);
        var summary = abcReport.Summary;

        if (summary.TotalProducts == 0)
        {
            return new ChartDataResponse
            {
                Categories = new List<string> { "Sem dados" },
                Series = new List<ChartSeriesDto> { new() { Name = "Receita", Data = new List<decimal> { 0 } } },
                Subtitle = "Nenhum dado de vendas no período",
                PeriodLabel = periodLabel
            };
        }

        return new ChartDataResponse
        {
            Categories = new List<string> 
            { 
                $"Classe A ({summary.ClassACount} itens)", 
                $"Classe B ({summary.ClassBCount} itens)", 
                $"Classe C ({summary.ClassCCount} itens)" 
            },
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Receita", Data = new List<decimal> 
                { 
                    summary.ClassARevenue, 
                    summary.ClassBRevenue, 
                    summary.ClassCRevenue 
                } }
            },
            Subtitle = $"Total: {CurrencyFormatService.FormatStatic(summary.TotalRevenue)} | A: {summary.ClassAPercentage:N1}% | B: {summary.ClassBPercentage:N1}% | C: {summary.ClassCPercentage:N1}%",
            PeriodLabel = periodLabel,
            DynamicDescription = $"Análise Curva ABC baseada em vendas de {periodLabel}"
        };
    }
}
