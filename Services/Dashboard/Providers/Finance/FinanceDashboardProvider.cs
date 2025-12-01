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
            Description = "Projeção de entradas vs saídas próximos 30 dias",
            ChartType = DashboardChartType.Area,
            Icon = "mdi-cash",
            Unit = "R$",
            RequiredRoles = new[] { "Financeiro", "Administrador" }
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "accounts-payable-summary",
            Title = "Resumo Contas a Pagar",
            Description = "Total pendente, vencido e pago",
            ChartType = DashboardChartType.Donut,
            Icon = "mdi-arrow-down-bold-circle",
            Unit = "R$",
            RequiredRoles = new[] { "Financeiro", "Administrador" }
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "accounts-receivable-summary",
            Title = "Resumo Contas a Receber",
            Description = "Total pendente, vencido e recebido",
            ChartType = DashboardChartType.Donut,
            Icon = "mdi-arrow-up-bold-circle",
            Unit = "R$",
            RequiredRoles = new[] { "Financeiro", "Administrador" }
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "aging-analysis",
            Title = "Análise de Aging",
            Description = "Distribuição de contas vencidas por período",
            ChartType = DashboardChartType.Bar,
            Icon = "mdi-calendar-alert",
            Unit = "R$",
            RequiredRoles = new[] { "Financeiro", "Administrador" }
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "top-suppliers",
            Title = "Top Fornecedores",
            Description = "Fornecedores com maior volume de compras",
            ChartType = DashboardChartType.Bar,
            Icon = "mdi-truck",
            Unit = "R$",
            RequiredRoles = new[] { "Financeiro", "Compras", "Administrador" }
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "top-customers",
            Title = "Top Clientes",
            Description = "Clientes com maior volume de vendas",
            ChartType = DashboardChartType.Bar,
            Icon = "mdi-account-group",
            Unit = "R$",
            RequiredRoles = new[] { "Financeiro", "Vendas", "Administrador" }
        },
        new DashboardWidgetDefinition
        {
            ProviderKey = Key,
            WidgetKey = "cashflow-alerts",
            Title = "Alertas Fluxo de Caixa",
            Description = "Dias projetados com saldo negativo",
            ChartType = DashboardChartType.Bar,
            Icon = "mdi-alert-circle",
            Unit = "R$",
            RequiredRoles = new[] { "Financeiro", "Gerente", "Administrador" }
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
        var dashboardData = await _financialService.GetDashboardDataAsync();
        
        // Get next 30 days projection
        var categories = dashboardData.CashFlowProjection
            .Take(30)
            .Select(x => x.Date.ToString("dd/MM"))
            .ToList();
            
        var revenues = dashboardData.CashFlowProjection
            .Take(30)
            .Select(x => x.Revenue)
            .ToList();
            
        var expenses = dashboardData.CashFlowProjection
            .Take(30)
            .Select(x => x.Expense)
            .ToList();

        var netTotal = revenues.Sum() - expenses.Sum();
        
        return new ChartDataResponse
        {
            Categories = categories,
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Entradas", Data = revenues },
                new() { Name = "Saídas", Data = expenses }
            },
            Subtitle = $"Saldo projetado: {netTotal:C2}"
        };
    }

    private async Task<ChartDataResponse> GetAccountsPayableSummaryAsync(DashboardQuery query, CancellationToken ct)
    {
        var dashboardData = await _financialService.GetDashboardDataAsync();
        
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
            Subtitle = $"Total: {dashboardData.TotalPayable:C2} | {dashboardData.PayablesCount} contas abertas"
        };
    }

    private async Task<ChartDataResponse> GetAccountsReceivableSummaryAsync(DashboardQuery query, CancellationToken ct)
    {
        var dashboardData = await _financialService.GetDashboardDataAsync();
        
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
            Subtitle = $"Total: {dashboardData.TotalReceivable:C2} | {dashboardData.ReceivablesCount} contas abertas"
        };
    }

    private async Task<ChartDataResponse> GetAgingAnalysisAsync(DashboardQuery query, CancellationToken ct)
    {
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
            Subtitle = $"Total vencido: {totalOverdue:C2}"
        };
    }

    private async Task<ChartDataResponse> GetTopSuppliersAsync(DashboardQuery query, CancellationToken ct)
    {
        var dashboardData = await _financialService.GetDashboardDataAsync();
        
        if (!dashboardData.TopSuppliers.Any())
        {
            return new ChartDataResponse
            {
                Categories = new List<string> { "Sem dados" },
                Series = new List<ChartSeriesDto> { new() { Name = "Valor", Data = new List<decimal> { 0 } } },
                Subtitle = "Nenhum fornecedor encontrado"
            };
        }
        
        return new ChartDataResponse
        {
            Categories = dashboardData.TopSuppliers.Select(x => x.Name).ToList(),
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Total Compras", Data = dashboardData.TopSuppliers.Select(x => x.TotalAmount).ToList() }
            },
            Subtitle = $"Top {dashboardData.TopSuppliers.Count} fornecedores"
        };
    }

    private async Task<ChartDataResponse> GetTopCustomersAsync(DashboardQuery query, CancellationToken ct)
    {
        var dashboardData = await _financialService.GetDashboardDataAsync();
        
        if (!dashboardData.TopCustomers.Any())
        {
            return new ChartDataResponse
            {
                Categories = new List<string> { "Sem dados" },
                Series = new List<ChartSeriesDto> { new() { Name = "Valor", Data = new List<decimal> { 0 } } },
                Subtitle = "Nenhum cliente encontrado"
            };
        }
        
        return new ChartDataResponse
        {
            Categories = dashboardData.TopCustomers.Select(x => x.Name).ToList(),
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Total Vendas", Data = dashboardData.TopCustomers.Select(x => x.TotalAmount).ToList() }
            },
            Subtitle = $"Top {dashboardData.TopCustomers.Count} clientes"
        };
    }

    private async Task<ChartDataResponse> GetCashflowAlertsAsync(DashboardQuery query, CancellationToken ct)
    {
        var dashboardData = await _financialService.GetDashboardDataAsync();
        var alerts = dashboardData.CashFlowAlerts;

        if (!alerts.Any())
        {
            return new ChartDataResponse
            {
                Categories = new List<string> { "Sem alertas" },
                Series = new List<ChartSeriesDto> { new() { Name = "Saldo", Data = new List<decimal> { 0 } } },
                Subtitle = "✅ Nenhum dia com saldo negativo projetado"
            };
        }

        // Show up to 10 days with negative balance
        var topAlerts = alerts.Take(10).ToList();
        var categories = topAlerts.Select(a => a.Date.ToString("dd/MM")).ToList();
        var balances = topAlerts.Select(a => Math.Abs(a.ProjectedBalance)).ToList();

        var criticalCount = alerts.Count(a => a.Severity == "Critical");
        var warningCount = alerts.Count(a => a.Severity == "Warning");
        
        var severityText = criticalCount > 0 
            ? $"⚠️ {criticalCount} crítico(s), {warningCount} alerta(s)"
            : $"⚠️ {warningCount} alerta(s)";

        return new ChartDataResponse
        {
            Categories = categories,
            Series = new List<ChartSeriesDto>
            {
                new() { Name = "Déficit Projetado", Data = balances }
            },
            Subtitle = $"{alerts.Count} dia(s) com saldo negativo | {severityText}"
        };
    }
}
