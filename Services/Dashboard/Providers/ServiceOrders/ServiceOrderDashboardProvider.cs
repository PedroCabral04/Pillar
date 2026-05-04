using Microsoft.EntityFrameworkCore;
using erp.Data;
using erp.DTOs.Dashboard;
using erp.Models.ServiceOrders;

namespace erp.Services.Dashboard.Providers.ServiceOrders;

public class ServiceOrderDashboardProvider : IDashboardWidgetProvider
{
    private readonly ApplicationDbContext _context;
    public const string Key = "service-orders";
    public string ProviderKey => Key;

    public ServiceOrderDashboardProvider(ApplicationDbContext context)
    {
        _context = context;
    }

    public IEnumerable<DashboardWidgetDefinition> GetWidgets() => new[]
    {
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "serviceorder-summary",
            Title = "Resumo de Ordens de Serviço",
            Description = "OS concluídas/entregues no período selecionado",
            ChartType = DashboardChartType.Bar,
            Icon = "mdi-wrench-clock",
            Unit = "R$",
            RequiredRoles = new[] { "Vendas", "AdminTenant", "SuperAdmin" }
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "serviceorder-by-month",
            Title = "OS por Mês",
            Description = "Total de OS concluídas agregadas por mês",
            ChartType = DashboardChartType.Bar,
            Icon = "mdi-chart-bar",
            Unit = "R$"
            , RequiredRoles = new[] { "Vendas", "AdminTenant", "SuperAdmin" }
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "serviceorder-by-status",
            Title = "OS por Status",
            Description = "Distribuição de ordens de serviço por status",
            ChartType = DashboardChartType.Donut,
            Icon = "mdi-chart-donut",
            Unit = "OS"
            , RequiredRoles = new[] { "Vendas", "AdminTenant", "SuperAdmin" }
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "serviceorder-top-services",
            Title = "Top Serviços Realizados",
            Description = "Serviços mais executados no período",
            ChartType = DashboardChartType.Pie,
            Icon = "mdi-star",
            Unit = "quantidade"
            , RequiredRoles = new[] { "Vendas", "AdminTenant", "SuperAdmin" }
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "serviceorder-device-types",
            Title = "Tipos de Aparelho",
            Description = "Distribuição de OS por tipo de aparelho",
            ChartType = DashboardChartType.Pie,
            Icon = "mdi-cellphone",
            Unit = "OS"
            , RequiredRoles = new[] { "Vendas", "AdminTenant", "SuperAdmin" }
        }
    };

    public Task<ChartDataResponse> QueryAsync(string widgetKey, DashboardQuery query, CancellationToken ct = default)
    {
        return widgetKey switch
        {
            "serviceorder-summary" => GetServiceOrderSummaryAsync(query, ct),
            "serviceorder-by-month" => GetServiceOrderByMonthAsync(query, ct),
            "serviceorder-by-status" => GetServiceOrderByStatusAsync(query, ct),
            "serviceorder-top-services" => GetTopServicesAsync(query, ct),
            "serviceorder-device-types" => GetDeviceTypesAsync(query, ct),
            _ => throw new KeyNotFoundException($"Widget '{widgetKey}' not found in provider '{Key}'.")
        };
    }

    private async Task<ChartDataResponse> GetServiceOrderSummaryAsync(DashboardQuery query, CancellationToken ct)
    {
        var startDate = query.From?.ToUniversalTime() ?? DateTime.UtcNow.Date;
        var endDate = query.To?.ToUniversalTime().AddDays(1) ?? DateTime.UtcNow.Date.AddDays(1);
        var periodLabel = DashboardDateUtils.FormatPeriodLabel(query.From, query.To);

        var completedStatus = new[] { ServiceOrderStatus.Completed.ToString(), ServiceOrderStatus.Delivered.ToString() };

        var orders = await _context.ServiceOrders
            .Where(o => completedStatus.Contains(o.Status) &&
                       o.ActualCompletionDate.HasValue &&
                       o.ActualCompletionDate.Value >= startDate &&
                       o.ActualCompletionDate.Value < endDate)
            .Select(o => new { o.Id, o.NetAmount })
            .ToListAsync(ct);

        var totalAmount = orders.Sum(o => o.NetAmount);
        var count = orders.Count;

        return new ChartDataResponse
        {
            Categories = new List<string> { "Total", "Quantidade" },
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Valor", Data = new List<decimal> { totalAmount, count } }
            },
            Subtitle = $"{count} OS(s) | Total: {CurrencyFormatService.FormatStatic(totalAmount)}",
            DynamicDescription = $"OS concluídas de {periodLabel}",
            PeriodLabel = periodLabel,
            Meta = new Dictionary<string, object>
            {
                { "TotalAmount", totalAmount },
                { "OrderCount", count },
                { "StartDate", startDate.ToString("yyyy-MM-dd") },
                { "EndDate", endDate.AddDays(-1).ToString("yyyy-MM-dd") }
            }
        };
    }

    private async Task<ChartDataResponse> GetServiceOrderByMonthAsync(DashboardQuery query, CancellationToken ct)
    {
        var startDate = query.From?.ToUniversalTime() ?? DateTime.UtcNow.AddMonths(-11).Date;
        var endDate = query.To?.ToUniversalTime() ?? DateTime.UtcNow.Date;
        var periodLabel = DashboardDateUtils.FormatPeriodLabel(query.From, query.To);
        var completedStatus = new[] { ServiceOrderStatus.Completed.ToString(), ServiceOrderStatus.Delivered.ToString() };

        var ordersByMonth = await _context.ServiceOrders
            .Where(o => completedStatus.Contains(o.Status) &&
                       o.ActualCompletionDate.HasValue &&
                       o.ActualCompletionDate.Value >= startDate &&
                       o.ActualCompletionDate.Value <= endDate)
            .GroupBy(o => new { o.ActualCompletionDate.Value.Year, o.ActualCompletionDate.Value.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Total = g.Sum(o => o.NetAmount)
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

        var ordersDict = ordersByMonth.ToDictionary(
            o => $"{o.Month}/{o.Year}",
            o => o.Total
        );

        var data = months.Select(m =>
        {
            var parts = m.Split('/');
            var monthNum = DateTime.ParseExact(parts[0], "MMM", System.Globalization.CultureInfo.CurrentCulture).Month;
            var year = int.Parse("20" + parts[1]);
            var key = $"{monthNum}/{year}";
            return ordersDict.GetValueOrDefault(key, 0);
        }).ToList();

        return new ChartDataResponse
        {
            Categories = months,
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Receita OS", Data = data }
            },
            Subtitle = $"Total: {CurrencyFormatService.FormatStatic(data.Sum(d => d))}",
            PeriodLabel = periodLabel,
            DynamicDescription = $"OS agregadas por mês de {periodLabel}"
        };
    }

    private async Task<ChartDataResponse> GetServiceOrderByStatusAsync(DashboardQuery query, CancellationToken ct)
    {
        var startDate = query.From ?? DateTime.UtcNow.AddMonths(-1).Date;
        var endDate = query.To ?? DateTime.UtcNow.Date;
        var periodLabel = DashboardDateUtils.FormatPeriodLabel(query.From, query.To);

        var ordersByStatus = await _context.ServiceOrders
            .Where(o => o.EntryDate >= startDate && o.EntryDate <= endDate)
            .GroupBy(o => o.Status)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count()
            })
            .ToListAsync(ct);

        if (!ordersByStatus.Any())
        {
            return new ChartDataResponse
            {
                Categories = new List<string> { "Sem dados" },
                Series = new List<ChartSeriesDto> { new() { Name = "OS", Data = new List<decimal> { 0 } } },
                Subtitle = "Nenhuma OS no período",
                PeriodLabel = periodLabel
            };
        }

        var statusDisplayMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Open"] = "Aberto",
            ["InProgress"] = "Em Andamento",
            ["WaitingCustomer"] = "Aguardando Cliente",
            ["WaitingParts"] = "Aguardando Peças",
            ["Completed"] = "Concluído",
            ["Delivered"] = "Entregue",
            ["Cancelled"] = "Cancelado"
        };

        var categories = ordersByStatus.Select(s => statusDisplayMap.GetValueOrDefault(s.Status, s.Status)).ToList();
        var values = ordersByStatus.Select(s => (decimal)s.Count).ToList();

        return new ChartDataResponse
        {
            Categories = categories,
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "OS", Data = values }
            },
            Subtitle = $"Total: {ordersByStatus.Sum(s => s.Count)} OS",
            PeriodLabel = periodLabel,
            DynamicDescription = $"OS por status de {periodLabel}"
        };
    }

    private async Task<ChartDataResponse> GetTopServicesAsync(DashboardQuery query, CancellationToken ct)
    {
        var startDate = query.From ?? DateTime.UtcNow.AddMonths(-1).Date;
        var endDate = query.To ?? DateTime.UtcNow.Date;
        var periodLabel = DashboardDateUtils.FormatPeriodLabel(query.From, query.To);
        var completedStatus = new[] { ServiceOrderStatus.Completed.ToString(), ServiceOrderStatus.Delivered.ToString() };

        var topServices = await _context.ServiceOrderItems
            .Include(i => i.ServiceOrder)
            .Where(i => i.ServiceOrder != null &&
                       completedStatus.Contains(i.ServiceOrder.Status) &&
                       i.ServiceOrder.ActualCompletionDate.HasValue &&
                       i.ServiceOrder.ActualCompletionDate.Value >= startDate &&
                       i.ServiceOrder.ActualCompletionDate.Value <= endDate)
            .GroupBy(i => i.Description)
            .Select(g => new
            {
                Description = g.Key,
                Count = g.Count(),
                Revenue = g.Sum(i => i.Price)
            })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync(ct);

        if (!topServices.Any())
        {
            return new ChartDataResponse
            {
                Categories = new List<string> { "Sem dados" },
                Series = new List<ChartSeriesDto> { new() { Name = "Quantidade", Data = new List<decimal> { 0 } } },
                Subtitle = "Nenhum serviço no período",
                PeriodLabel = periodLabel
            };
        }

        return new ChartDataResponse
        {
            Categories = topServices.Select(s => s.Description).ToList(),
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Quantidade", Data = topServices.Select(s => (decimal)s.Count).ToList() },
                new() { Name = "Receita", Data = topServices.Select(s => s.Revenue).ToList() }
            },
            Subtitle = $"Top {topServices.Count} serviços",
            PeriodLabel = periodLabel,
            DynamicDescription = $"Serviços mais realizados de {periodLabel}"
        };
    }

    private async Task<ChartDataResponse> GetDeviceTypesAsync(DashboardQuery query, CancellationToken ct)
    {
        var startDate = query.From ?? DateTime.UtcNow.AddMonths(-1).Date;
        var endDate = query.To ?? DateTime.UtcNow.Date;
        var periodLabel = DashboardDateUtils.FormatPeriodLabel(query.From, query.To);

        var deviceTypes = await _context.ServiceOrders
            .Where(o => o.EntryDate >= startDate && o.EntryDate <= endDate)
            .GroupBy(o => o.DeviceType)
            .Select(g => new
            {
                DeviceType = g.Key,
                Count = g.Count()
            })
            .ToListAsync(ct);

        if (!deviceTypes.Any())
        {
            return new ChartDataResponse
            {
                Categories = new List<string> { "Sem dados" },
                Series = new List<ChartSeriesDto> { new() { Name = "OS", Data = new List<decimal> { 0 } } },
                Subtitle = "Nenhuma OS no período",
                PeriodLabel = periodLabel
            };
        }

        var typeDisplayMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Smartphone"] = "Smartphone",
            ["Tablet"] = "Tablet",
            ["Notebook"] = "Notebook",
            ["Desktop"] = "Desktop",
            ["Smartwatch"] = "Smartwatch",
            ["VideoGame"] = "Video Game",
            ["TV"] = "TV",
            ["Other"] = "Outro"
        };

        var categories = deviceTypes
            .Select(d => typeDisplayMap.GetValueOrDefault(d.DeviceType ?? "Outro", d.DeviceType ?? "Outro"))
            .ToList();
        var values = deviceTypes.Select(d => (decimal)d.Count).ToList();

        return new ChartDataResponse
        {
            Categories = categories,
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "OS", Data = values }
            },
            Subtitle = $"Total: {deviceTypes.Sum(d => d.Count)} OS",
            PeriodLabel = periodLabel,
            DynamicDescription = $"Distribuição por tipo de aparelho de {periodLabel}"
        };
    }
}
