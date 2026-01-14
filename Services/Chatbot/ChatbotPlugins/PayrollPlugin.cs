using System.ComponentModel;
using Microsoft.SemanticKernel;
using erp.Services.Payroll;
using erp.Models.Payroll;
using erp.Models.TimeTracking;

namespace erp.Services.Chatbot.ChatbotPlugins;

/// <summary>
/// Plugin para gerenciar folha de pagamento através do chatbot
/// </summary>
public class PayrollPlugin
{
    private readonly IPayrollService _payrollService;

    public PayrollPlugin(IPayrollService payrollService)
    {
        _payrollService = payrollService;
    }

    [KernelFunction, Description("Lista os períodos de folha de pagamento")]
    public async Task<string> ListPayrollPeriods(
        [Description("Ano para filtrar (opcional, ex: 2024)")] int? year = null,
        [Description("Status para filtrar: Draft, Calculated, Approved, Paid (opcional)")] string? status = null)
    {
        try
        {
            PayrollPeriodStatus? statusFilter = null;
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<PayrollPeriodStatus>(status, ignoreCase: true, out var parsedStatus))
            {
                statusFilter = parsedStatus;
            }

            var periods = await _payrollService.GetPeriodsAsync(year, statusFilter);

            if (!periods.Any())
            {
                var filterText = year.HasValue ? $" de {year}" : "";
                return $"📋 Não há períodos de folha{filterText}.";
            }

            var list = periods.Take(12).Select(p =>
                $"| {p.Id} | {GetMonthName(p.ReferenceMonth)}/{p.ReferenceYear} | {GetStatusText(p.Status)} | {p.Results.Count} |"
            );

            var yearText = year.HasValue ? $" de {year}" : "";
            var remaining = periods.Count - 12;
            var moreText = remaining > 0 ? $"\n\n*...e mais {remaining} períodos.*" : "";

            return $"""
                📋 **Períodos de Folha{yearText}** ({periods.Count} total)
                
                | ID | Referência | Status | Funcs |
                |----|------------|--------|-------|
                {string.Join("\n", list)}{moreText}
                """;
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao listar períodos: {ex.Message}";
        }
    }

    [KernelFunction, Description("Obtém detalhes de um período específico de folha de pagamento")]
    public async Task<string> GetPayrollPeriodDetails(
        [Description("ID do período de folha de pagamento")] int periodId)
    {
        try
        {
            var period = await _payrollService.GetPeriodAsync(periodId);

            if (period == null)
            {
                return $"🔍 Período de folha **#{periodId}** não encontrado.";
            }

            var totalBruto = period.Results.Sum(r => r.GrossAmount);
            var totalLiquido = period.Results.Sum(r => r.NetAmount);
            var totalDescontos = period.Results.Sum(r => r.TotalDeductions);

            return $"""
                📋 **Folha de {GetMonthName(period.ReferenceMonth)}/{period.ReferenceYear}**
                
                | Métrica | Valor |
                |---------|-------|
                | **Status** | {GetStatusText(period.Status)} |
                | **Funcionários** | {period.Results.Count} |
                | **Total Bruto** | R$ {totalBruto:N2} |
                | **Descontos** | R$ {totalDescontos:N2} |
                | **Total Líquido** | R$ {totalLiquido:N2} |
                
                {(period.Notes != null ? $"> **Obs:** {period.Notes}" : "")}
                """;
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao buscar detalhes: {ex.Message}";
        }
    }

    [KernelFunction, Description("Obtém o resumo da folha de pagamento do período atual ou mais recente")]
    public async Task<string> GetCurrentPayrollSummary()
    {
        try
        {
            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;

            var periods = await _payrollService.GetPeriodsAsync(currentYear, null);
            
            var period = periods.FirstOrDefault(p => p.ReferenceMonth == currentMonth && p.ReferenceYear == currentYear)
                      ?? periods.FirstOrDefault();

            if (period == null)
            {
                return $"📋 Não há períodos de folha em {currentYear}.";
            }

            var totalBruto = period.Results.Sum(r => r.GrossAmount);
            var totalLiquido = period.Results.Sum(r => r.NetAmount);
            var totalDescontos = period.Results.Sum(r => r.TotalDeductions);

            return $"""
                📋 **Folha Atual — {GetMonthName(period.ReferenceMonth)}/{period.ReferenceYear}**
                
                | Métrica | Valor |
                |---------|-------|
                | **Status** | {GetStatusText(period.Status)} |
                | **Funcionários** | {period.Results.Count} |
                | **Bruto** | R$ {totalBruto:N2} |
                | **Descontos** | R$ {totalDescontos:N2} |
                | **Líquido** | R$ {totalLiquido:N2} |
                """;
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao buscar resumo: {ex.Message}";
        }
    }

    [KernelFunction, Description("Lista os funcionários e seus valores em um período específico de folha")]
    public async Task<string> ListPayrollEmployees(
        [Description("ID do período de folha de pagamento")] int periodId,
        [Description("Número máximo de funcionários a exibir")] int maxResults = 10)
    {
        try
        {
            var period = await _payrollService.GetPeriodAsync(periodId);

            if (period == null)
            {
                return $"🔍 Período de folha **#{periodId}** não encontrado.";
            }

            if (!period.Results.Any())
            {
                return $"📋 O período não possui resultados calculados.";
            }

            var list = period.Results
                .OrderByDescending(r => r.NetAmount)
                .Take(maxResults)
                .Select(r =>
                    $"| {r.EmployeeNameSnapshot ?? $"#{r.EmployeeId}"} | R$ {r.GrossAmount:N2} | R$ {r.NetAmount:N2} |"
                );

            var remaining = period.Results.Count - maxResults;
            var moreText = remaining > 0 ? $"\n\n*...e mais {remaining} funcionários.*" : "";

            return $"""
                👥 **Folha de {GetMonthName(period.ReferenceMonth)}/{period.ReferenceYear}** ({period.Results.Count} funcs)
                
                | Funcionário | Bruto | Líquido |
                |-------------|-------|----------|
                {string.Join("\n", list)}{moreText}
                """;
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao listar funcionários: {ex.Message}";
        }
    }

    [KernelFunction, Description("Obtém estatísticas anuais de folha de pagamento")]
    public async Task<string> GetPayrollYearlyStatistics(
        [Description("Ano para obter estatísticas (padrão: ano atual)")] int? year = null)
    {
        try
        {
            var targetYear = year ?? DateTime.UtcNow.Year;
            var periods = await _payrollService.GetPeriodsAsync(targetYear, null);

            if (!periods.Any())
            {
                return $"📊 Não há dados de folha para {targetYear}.";
            }

            var totalBrutoAnual = periods.Sum(p => p.Results.Sum(r => r.GrossAmount));
            var totalLiquidoAnual = periods.Sum(p => p.Results.Sum(r => r.NetAmount));
            var totalDescontosAnual = periods.Sum(p => p.Results.Sum(r => r.TotalDeductions));
            var mediaFuncionarios = periods.Any() ? periods.Average(p => p.Results.Count) : 0;
            var periodosPagos = periods.Count(p => p.Status == PayrollPeriodStatus.Paid);

            return $"""
                📊 **Estatísticas de Folha — {targetYear}**
                
                | Métrica | Valor |
                |---------|-------|
                | **Períodos** | {periods.Count} ({periodosPagos} pagos) |
                | **Total Bruto** | R$ {totalBrutoAnual:N2} |
                | **Total Descontos** | R$ {totalDescontosAnual:N2} |
                | **Total Líquido** | R$ {totalLiquidoAnual:N2} |
                | **Média Funcs/Mês** | {mediaFuncionarios:F0} |
                | **Folha Mensal Média** | R$ {(periods.Count > 0 ? totalLiquidoAnual / periods.Count : 0):N2} |
                """;
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao obter estatísticas: {ex.Message}";
        }
    }

    [KernelFunction, Description("Lista períodos de folha pendentes (não calculados, não aprovados ou não pagos)")]
    public async Task<string> GetPendingPayrollPeriods()
    {
        try
        {
            var currentYear = DateTime.UtcNow.Year;
            var periods = await _payrollService.GetPeriodsAsync(currentYear, null);

            var pendingPeriods = periods.Where(p => p.Status != PayrollPeriodStatus.Paid).ToList();

            if (!pendingPeriods.Any())
            {
                return $"✅ Todos os períodos de {currentYear} estão pagos!";
            }

            var draft = pendingPeriods.Where(p => p.Status == PayrollPeriodStatus.Draft).ToList();
            var calculated = pendingPeriods.Where(p => p.Status == PayrollPeriodStatus.Calculated).ToList();
            var approved = pendingPeriods.Where(p => p.Status == PayrollPeriodStatus.Approved).ToList();

            var result = $"⏳ **Períodos Pendentes de {currentYear}**\n\n";

            if (draft.Any())
            {
                var items = draft.Select(p => $"| {p.Id} | {GetMonthName(p.ReferenceMonth)}/{p.ReferenceYear} |");
                result += $"""
                    📝 **Aguardando Cálculo** ({draft.Count})
                    
                    | ID | Referência |
                    |----|------------|
                    {string.Join("\n", items)}
                    
                    """;
            }

            if (calculated.Any())
            {
                var items = calculated.Select(p => $"| {p.Id} | {GetMonthName(p.ReferenceMonth)}/{p.ReferenceYear} |");
                result += $"""
                    ✅ **Aguardando Aprovação** ({calculated.Count})
                    
                    | ID | Referência |
                    |----|------------|
                    {string.Join("\n", items)}
                    
                    """;
            }

            if (approved.Any())
            {
                var items = approved.Select(p => $"| {p.Id} | {GetMonthName(p.ReferenceMonth)}/{p.ReferenceYear} |");
                result += $"""
                    💳 **Aguardando Pagamento** ({approved.Count})
                    
                    | ID | Referência |
                    |----|------------|
                    {string.Join("\n", items)}
                    """;
            }

            return result.TrimEnd();
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao buscar períodos pendentes: {ex.Message}";
        }
    }

    private static string GetStatusText(PayrollPeriodStatus status) => status switch
    {
        PayrollPeriodStatus.Draft => "📝 Rascunho",
        PayrollPeriodStatus.Calculated => "🔢 Calculado",
        PayrollPeriodStatus.Approved => "✅ Aprovado",
        PayrollPeriodStatus.Paid => "💰 Pago",
        _ => status.ToString()
    };

    private static string GetMonthName(int month) => month switch
    {
        1 => "Janeiro",
        2 => "Fevereiro",
        3 => "Março",
        4 => "Abril",
        5 => "Maio",
        6 => "Junho",
        7 => "Julho",
        8 => "Agosto",
        9 => "Setembro",
        10 => "Outubro",
        11 => "Novembro",
        12 => "Dezembro",
        _ => month.ToString()
    };
}
