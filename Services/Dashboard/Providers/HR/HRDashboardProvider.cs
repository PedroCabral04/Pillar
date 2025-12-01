using Microsoft.EntityFrameworkCore;
using erp.Data;
using erp.DTOs.Dashboard;
using erp.Models.Identity;

namespace erp.Services.Dashboard.Providers.HR;

public class HRDashboardProvider : IDashboardWidgetProvider
{
    private readonly ApplicationDbContext _context;
    public const string Key = "hr";
    public string ProviderKey => Key;

    public HRDashboardProvider(ApplicationDbContext context)
    {
        _context = context;
    }

    public IEnumerable<DashboardWidgetDefinition> GetWidgets() => new[]
    {
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "employees-count",
            Title = "Contagem de Funcionários",
            Description = "Funcionários ativos por status",
            ChartType = DashboardChartType.Donut,
            Icon = "mdi-account-group",
            Unit = "funcionários",
            RequiredRoles = new[] { "RH", "Administrador" }
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "department-headcount",
            Title = "Funcionários por Departamento",
            Description = "Distribuição de funcionários por departamento",
            ChartType = DashboardChartType.Bar,
            Icon = "mdi-domain",
            Unit = "funcionários",
            RequiredRoles = new[] { "RH", "Administrador" }
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "recent-hires",
            Title = "Contratações Recentes",
            Description = "Funcionários contratados nos últimos 12 meses",
            ChartType = DashboardChartType.Line,
            Icon = "mdi-account-plus",
            Unit = "contratações",
            RequiredRoles = new[] { "RH", "Administrador" }
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "payroll-summary",
            Title = "Resumo da Folha",
            Description = "Valores totais do último período de folha",
            ChartType = DashboardChartType.Bar,
            Icon = "mdi-cash-multiple",
            Unit = "R$",
            RequiredRoles = new[] { "RH", "Financeiro", "Administrador" }
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "positions-overview",
            Title = "Cargos na Empresa",
            Description = "Distribuição de funcionários por cargo",
            ChartType = DashboardChartType.Pie,
            Icon = "mdi-briefcase",
            Unit = "funcionários",
            RequiredRoles = new[] { "RH", "Administrador" }
        }
    };

    public Task<ChartDataResponse> QueryAsync(string widgetKey, DashboardQuery query, CancellationToken ct = default)
    {
        return widgetKey switch
        {
            "employees-count" => GetEmployeesCountAsync(query, ct),
            "department-headcount" => GetDepartmentHeadcountAsync(query, ct),
            "recent-hires" => GetRecentHiresAsync(query, ct),
            "payroll-summary" => GetPayrollSummaryAsync(query, ct),
            "positions-overview" => GetPositionsOverviewAsync(query, ct),
            _ => throw new KeyNotFoundException($"Widget '{widgetKey}' not found in provider '{Key}'.")
        };
    }

    private async Task<ChartDataResponse> GetEmployeesCountAsync(DashboardQuery query, CancellationToken ct)
    {
        var employees = await _context.Set<ApplicationUser>()
            .Where(u => u.HireDate != null) // Only employees, not just users
            .GroupBy(u => u.EmploymentStatus ?? "Ativo")
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count()
            })
            .ToListAsync(ct);

        if (!employees.Any())
        {
            return new ChartDataResponse
            {
                Categories = new List<string> { "Sem dados" },
                Series = new List<ChartSeriesDto> { new() { Name = "Funcionários", Data = new List<decimal> { 0 } } },
                Subtitle = "Nenhum funcionário cadastrado"
            };
        }

        var categories = employees.Select(e => e.Status).ToList();
        var data = employees.Select(e => (decimal)e.Count).ToList();
        var total = employees.Sum(e => e.Count);

        return new ChartDataResponse
        {
            Categories = categories,
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Funcionários", Data = data }
            },
            Subtitle = $"Total: {total} funcionários"
        };
    }

    private async Task<ChartDataResponse> GetDepartmentHeadcountAsync(DashboardQuery query, CancellationToken ct)
    {
        var headcount = await _context.Set<ApplicationUser>()
            .Where(u => u.HireDate != null && u.DepartmentId != null && (u.EmploymentStatus == null || u.EmploymentStatus == "Ativo"))
            .Include(u => u.Department)
            .GroupBy(u => u.Department!.Name)
            .Select(g => new
            {
                Department = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync(ct);

        if (!headcount.Any())
        {
            return new ChartDataResponse
            {
                Categories = new List<string> { "Sem dados" },
                Series = new List<ChartSeriesDto> { new() { Name = "Funcionários", Data = new List<decimal> { 0 } } },
                Subtitle = "Nenhum departamento com funcionários"
            };
        }

        return new ChartDataResponse
        {
            Categories = headcount.Select(h => h.Department ?? "Sem departamento").ToList(),
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Funcionários", Data = headcount.Select(h => (decimal)h.Count).ToList() }
            },
            Subtitle = $"Top {headcount.Count} departamentos"
        };
    }

    private async Task<ChartDataResponse> GetRecentHiresAsync(DashboardQuery query, CancellationToken ct)
    {
        var startDate = (query.From ?? DateTime.Now.AddMonths(-11).Date).ToUniversalTime();
        var endDate = (query.To ?? DateTime.Now.Date).ToUniversalTime();

        var hires = await _context.Set<ApplicationUser>()
            .Where(u => u.HireDate != null && u.HireDate >= startDate && u.HireDate <= endDate)
            .GroupBy(u => new { u.HireDate!.Value.Year, u.HireDate.Value.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Count = g.Count()
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

        var hiresDict = hires.ToDictionary(
            h => $"{h.Month}/{h.Year}",
            h => h.Count
        );

        var data = months.Select(m =>
        {
            var parts = m.Split('/');
            var monthNum = DateTime.ParseExact(parts[0], "MMM", System.Globalization.CultureInfo.CurrentCulture).Month;
            var year = int.Parse("20" + parts[1]);
            var key = $"{monthNum}/{year}";
            return (decimal)hiresDict.GetValueOrDefault(key, 0);
        }).ToList();

        return new ChartDataResponse
        {
            Categories = months,
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Contratações", Data = data }
            },
            Subtitle = $"Total: {data.Sum()} contratações no período"
        };
    }

    private async Task<ChartDataResponse> GetPayrollSummaryAsync(DashboardQuery query, CancellationToken ct)
    {
        var latestPeriod = await _context.PayrollPeriods
            .OrderByDescending(p => p.ReferenceYear)
            .ThenByDescending(p => p.ReferenceMonth)
            .FirstOrDefaultAsync(ct);

        if (latestPeriod == null)
        {
            return new ChartDataResponse
            {
                Categories = new List<string> { "Sem dados" },
                Series = new List<ChartSeriesDto> { new() { Name = "Valor", Data = new List<decimal> { 0 } } },
                Subtitle = "Nenhuma folha processada"
            };
        }

        return new ChartDataResponse
        {
            Categories = new List<string> { "Bruto", "Líquido", "INSS", "IRRF", "Custo Empresa" },
            Series = new List<ChartSeriesDto>
            {
                new() 
                { 
                    Name = "Valor", 
                    Data = new List<decimal> 
                    { 
                        latestPeriod.TotalGrossAmount, 
                        latestPeriod.TotalNetAmount, 
                        latestPeriod.TotalInssAmount, 
                        latestPeriod.TotalIrrfAmount, 
                        latestPeriod.TotalEmployerCost 
                    } 
                }
            },
            Subtitle = $"Competência: {latestPeriod.ReferenceMonth:00}/{latestPeriod.ReferenceYear}"
        };
    }

    private async Task<ChartDataResponse> GetPositionsOverviewAsync(DashboardQuery query, CancellationToken ct)
    {
        var positions = await _context.Set<ApplicationUser>()
            .Where(u => u.HireDate != null && u.PositionId != null && (u.EmploymentStatus == null || u.EmploymentStatus == "Ativo"))
            .Include(u => u.Position)
            .GroupBy(u => u.Position!.Title)
            .Select(g => new
            {
                Position = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync(ct);

        if (!positions.Any())
        {
            return new ChartDataResponse
            {
                Categories = new List<string> { "Sem dados" },
                Series = new List<ChartSeriesDto> { new() { Name = "Funcionários", Data = new List<decimal> { 0 } } },
                Subtitle = "Nenhum cargo com funcionários"
            };
        }

        return new ChartDataResponse
        {
            Categories = positions.Select(p => p.Position ?? "Sem cargo").ToList(),
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Funcionários", Data = positions.Select(p => (decimal)p.Count).ToList() }
            },
            Subtitle = $"Top {positions.Count} cargos"
        };
    }
}
