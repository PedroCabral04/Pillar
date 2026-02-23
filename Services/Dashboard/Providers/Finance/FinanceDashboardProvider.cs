using erp.DTOs.Dashboard;
using erp.Services.Financial;

namespace erp.Services.Dashboard.Providers.Finance;

public class FinanceDashboardProvider : IDashboardWidgetProvider
{
    private readonly IFinancialDashboardService _financialService;
    public const string Key = "finance";
    public string ProviderKey => Key;

    public FinanceDashboardProvider(IFinancialDashboardService financialService)
    {
        _financialService = financialService;
    }

    public IEnumerable<DashboardWidgetDefinition> GetWidgets() => new[]
    {
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "cashflow",
            Title = "Fluxo de Caixa",
            Description = "Projeção de entradas vs saídas no período",
            ChartType = DashboardChartType.Area,
            Icon = "mdi-cash",
            Unit = "R$",
            RequiredRoles = new[] { "Financeiro", "AdminTenant", "SuperAdmin" }
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "accounts-payable-summary",
            Title = "Resumo Contas a Pagar",
            Description = "Total pendente, vencido e pago no período",
            ChartType = DashboardChartType.Donut,
            Icon = "mdi-arrow-down-bold-circle",
            Unit = "R$",
            RequiredRoles = new[] { "Financeiro", "AdminTenant", "SuperAdmin" }
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "accounts-receivable-summary",
            Title = "Resumo Contas a Receber",
            Description = "Total pendente, vencido e recebido no período",
            ChartType = DashboardChartType.Donut,
            Icon = "mdi-arrow-up-bold-circle",
            Unit = "R$",
            RequiredRoles = new[] { "Financeiro", "AdminTenant", "SuperAdmin" }
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "aging-analysis",
            Title = "Análise de Aging",
            Description = "Distribuição de contas vencidas por período (estado atual)",
            ChartType = DashboardChartType.Bar,
            Icon = "mdi-calendar-alert",
            Unit = "R$",
            RequiredRoles = new[] { "Financeiro", "AdminTenant", "SuperAdmin" }
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "top-suppliers",
            Title = "Top Fornecedores",
            Description = "Fornecedores com maior volume de compras no período",
            ChartType = DashboardChartType.Bar,
            Icon = "mdi-truck",
            Unit = "R$",
            RequiredRoles = new[] { "Financeiro", "Compras", "AdminTenant", "SuperAdmin" }
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "top-customers",
            Title = "Top Clientes",
            Description = "Clientes com maior volume de vendas no período",
            ChartType = DashboardChartType.Bar,
            Icon = "mdi-account-group",
            Unit = "R$",
            RequiredRoles = new[] { "Financeiro", "Vendas", "AdminTenant", "SuperAdmin" }
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "cashflow-alerts",
            Title = "Alertas Fluxo de Caixa",
            Description = "Dias projetados com saldo negativo no período",
            ChartType = DashboardChartType.Bar,
            Icon = "mdi-alert-circle",
            Unit = "R$",
            RequiredRoles = new[] { "Financeiro", "Gerente", "AdminTenant", "SuperAdmin" }
        }
    };

    public async Task<ChartDataResponse> QueryAsync(string widgetKey, DashboardQuery query, CancellationToken ct = default)
    {
        return widgetKey switch
        {
            "cashflow" => await GetCashflowAsync(query, ct),
            "accounts-payable-summary" => await GetAccountsPayableSummaryAsync(query, ct),
            "accounts-receivable-summary" => await GetAccountsReceivableSummaryAsync(query, ct),
            "aging-analysis" => await GetAgingAnalysisAsync(query, ct),
            "top-suppliers" => await GetTopSuppliersAsync(query, ct),
            "top-customers" => await GetTopCustomersAsync(query, ct),
            "cashflow-alerts" => await GetCashflowAlertsAsync(query, ct),
            _ => throw new KeyNotFoundException($"Widget '{widgetKey}' not found in provider '{Key}'.")
        };
    }
    

    private async Task<ChartDataResponse> GetCashflowAsync(DashboardQuery query, CancellationToken ct)
    {
        var periodLabel = DashboardDateUtils.FormatPeriodLabel(query.From, query.To);
        var dashboardData = await _financialService.GetDashboardDataAsync(query.From, query.To);
        
        var categories = dashboardData.CashFlowProjection
            .Select(x => x.Date.ToString("dd/MM"))
            .ToList();
            
        var revenues = dashboardData.CashFlowProjection
            .Select(x => x.Revenue)
            .ToList();
            
        var expenses = dashboardData.CashFlowProjection
            .Select(x => x.Expense)
            .ToList();

        var netTotal = revenues.Sum() - expenses.Sum();
        var daysCount = dashboardData.CashFlowProjection.Count;
        
        return new ChartDataResponse
        {
            Categories = categories,
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Entradas", Data = revenues },
                new() { Name = "Saídas", Data = expenses }
            },
            Subtitle = $"Saldo projetado: {CurrencyFormatService.FormatStatic(netTotal)}",
            PeriodLabel = periodLabel,
            DynamicDescription = $"Projeção de fluxo de caixa para {daysCount} dias"
        };
    }

    private async Task<ChartDataResponse> GetAccountsPayableSummaryAsync(DashboardQuery query, CancellationToken ct)
    {
        var periodLabel = DashboardDateUtils.FormatPeriodLabel(query.From, query.To);
        var dashboardData = await _financialService.GetDashboardDataAsync(query.From, query.To);
        
        return new ChartDataResponse
        {
            Categories = new List<string> { "Pendente", "Vencido", "Pago" },
            Series = new List<ChartSeriesDto>
            {
                new() 
                { 
                    Name = "Valor", 
                    Data = new List<decimal> 
                    { 
                        dashboardData.TotalPayablePending, 
                        dashboardData.TotalPayableOverdue, 
                        dashboardData.TotalPayablePaid 
                    } 
                }
            },
            Subtitle = $"Total: {CurrencyFormatService.FormatStatic(dashboardData.TotalPayable)} | {dashboardData.PayablesCount} contas abertas",
            PeriodLabel = periodLabel,
            DynamicDescription = $"Contas a pagar emitidas de {periodLabel}"
        };
    }

    private async Task<ChartDataResponse> GetAccountsReceivableSummaryAsync(DashboardQuery query, CancellationToken ct)
    {
        var periodLabel = DashboardDateUtils.FormatPeriodLabel(query.From, query.To);
        var dashboardData = await _financialService.GetDashboardDataAsync(query.From, query.To);
        
        return new ChartDataResponse
        {
            Categories = new List<string> { "Pendente", "Vencido", "Recebido" },
            Series = new List<ChartSeriesDto>
            {
                new() 
                { 
                    Name = "Valor", 
                    Data = new List<decimal> 
                    { 
                        dashboardData.TotalReceivablePending, 
                        dashboardData.TotalReceivableOverdue, 
                        dashboardData.TotalReceivablePaid 
                    } 
                }
            },
            Subtitle = $"Total: {CurrencyFormatService.FormatStatic(dashboardData.TotalReceivable)} | {dashboardData.ReceivablesCount} contas abertas",
            PeriodLabel = periodLabel,
            DynamicDescription = $"Contas a receber emitidas de {periodLabel}"
        };
    }

    private async Task<ChartDataResponse> GetAgingAnalysisAsync(DashboardQuery query, CancellationToken ct)
    {
        // Aging analysis always uses current state (not filtered by period)
        var dashboardData = await _financialService.GetDashboardDataAsync();
        
        var payableAging = dashboardData.PayablesAgingList;
        var receivableAging = dashboardData.ReceivablesAgingList;
        
        var categories = payableAging.Select(x => x.Period).ToList();
        var payableData = payableAging.Select(x => x.Amount).ToList();
        var receivableData = receivableAging.Select(x => x.Amount).ToList();
        
        var totalOverdue = payableData.Sum() + receivableData.Sum();
        
        return new ChartDataResponse
        {
            Categories = categories,
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "A Pagar Vencido", Data = payableData },
                new() { Name = "A Receber Vencido", Data = receivableData }
            },
            Subtitle = $"Total vencido: {CurrencyFormatService.FormatStatic(totalOverdue)}",
            IsCurrentStateWidget = true,
            DynamicDescription = "Análise de envelhecimento das contas vencidas (estado atual)"
        };
    }

    private async Task<ChartDataResponse> GetTopSuppliersAsync(DashboardQuery query, CancellationToken ct)
    {
        var periodLabel = DashboardDateUtils.FormatPeriodLabel(query.From, query.To);
        var dashboardData = await _financialService.GetDashboardDataAsync(query.From, query.To);
        
        if (!dashboardData.TopSuppliers.Any())
        {
            return new ChartDataResponse
            {
                Categories = new List<string> { "Sem dados" },
                Series = new List<ChartSeriesDto> { new() { Name = "Valor", Data = new List<decimal> { 0 } } },
                Subtitle = "Nenhum fornecedor no período",
                PeriodLabel = periodLabel
            };
        }
        
        return new ChartDataResponse
        {
            Categories = dashboardData.TopSuppliers.Select(x => x.Name).ToList(),
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Total Compras", Data = dashboardData.TopSuppliers.Select(x => x.TotalAmount).ToList() }
            },
            Subtitle = $"Top {dashboardData.TopSuppliers.Count} fornecedores",
            PeriodLabel = periodLabel,
            DynamicDescription = $"Principais fornecedores de {periodLabel}"
        };
    }

    private async Task<ChartDataResponse> GetTopCustomersAsync(DashboardQuery query, CancellationToken ct)
    {
        var periodLabel = DashboardDateUtils.FormatPeriodLabel(query.From, query.To);
        var dashboardData = await _financialService.GetDashboardDataAsync(query.From, query.To);
        
        if (!dashboardData.TopCustomers.Any())
        {
            return new ChartDataResponse
            {
                Categories = new List<string> { "Sem dados" },
                Series = new List<ChartSeriesDto> { new() { Name = "Valor", Data = new List<decimal> { 0 } } },
                Subtitle = "Nenhum cliente no período",
                PeriodLabel = periodLabel
            };
        }
        
        return new ChartDataResponse
        {
            Categories = dashboardData.TopCustomers.Select(x => x.Name).ToList(),
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Total Vendas", Data = dashboardData.TopCustomers.Select(x => x.TotalAmount).ToList() }
            },
            Subtitle = $"Top {dashboardData.TopCustomers.Count} clientes",
            PeriodLabel = periodLabel,
            DynamicDescription = $"Principais clientes de {periodLabel}"
        };
    }

    private async Task<ChartDataResponse> GetCashflowAlertsAsync(DashboardQuery query, CancellationToken ct)
    {
        var periodLabel = DashboardDateUtils.FormatPeriodLabel(query.From, query.To);
        var dashboardData = await _financialService.GetDashboardDataAsync(query.From, query.To);
        var alerts = dashboardData.CashFlowAlerts;

        if (!alerts.Any())
        {
            return new ChartDataResponse
            {
                Categories = new List<string> { "Sem alertas" },
                Series = new List<ChartSeriesDto> { new() { Name = "Saldo", Data = new List<decimal> { 0 } } },
                Subtitle = "Nenhum dia com saldo negativo projetado",
                PeriodLabel = periodLabel
            };
        }

        // Show up to 10 days with negative balance
        var topAlerts = alerts.Take(10).ToList();
        var categories = topAlerts.Select(a => a.Date.ToString("dd/MM")).ToList();
        var balances = topAlerts.Select(a => Math.Abs(a.ProjectedBalance)).ToList();

        var criticalCount = alerts.Count(a => a.Severity == "Critical");
        var warningCount = alerts.Count(a => a.Severity == "Warning");
        
        var severityText = criticalCount > 0 
            ? $"{criticalCount} crítico(s), {warningCount} alerta(s)"
            : $"{warningCount} alerta(s)";

        return new ChartDataResponse
        {
            Categories = categories,
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Déficit Projetado", Data = balances }
            },
            Subtitle = $"{alerts.Count} dia(s) com saldo negativo | {severityText}",
            PeriodLabel = periodLabel,
            DynamicDescription = $"Alertas de fluxo de caixa para {periodLabel}"
        };
    }
}
