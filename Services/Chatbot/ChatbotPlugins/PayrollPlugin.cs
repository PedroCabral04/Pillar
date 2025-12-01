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
                var filterText = year.HasValue ? $" para o ano {year}" : "";
                filterText += statusFilter.HasValue ? $" com status '{GetStatusText(statusFilter.Value)}'" : "";
                return $"📋 Não há períodos de folha de pagamento cadastrados{filterText}.";
            }

            var periodList = periods.Take(20).Select(p =>
                $"- **{GetMonthName(p.ReferenceMonth)}/{p.ReferenceYear}** (ID: {p.Id}) - Status: {GetStatusText(p.Status)} - Funcionários: {p.Results.Count}"
            );

            var yearText = year.HasValue ? $" de {year}" : "";
            return $"📋 **Períodos de Folha de Pagamento{yearText} ({periods.Count} total):**\n{string.Join("\n", periodList)}";
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao listar períodos de folha: {ex.Message}";
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
                return $"🔍 Período de folha com ID {periodId} não encontrado.";
            }

            var totalBruto = period.Results.Sum(r => r.GrossAmount);
            var totalLiquido = period.Results.Sum(r => r.NetAmount);
            var totalDescontos = period.Results.Sum(r => r.TotalDeductions);
            var totalProventos = period.Results.Sum(r => r.TotalEarnings);

            var statusInfo = period.Status switch
            {
                PayrollPeriodStatus.Approved => $" - Aprovado em: {period.ApprovedAt:dd/MM/yyyy}",
                PayrollPeriodStatus.Paid => $" - Pago em: {period.PaidAt:dd/MM/yyyy}",
                _ => ""
            };

            return $"📋 **Detalhes do Período de Folha:**\n\n" +
                   $"**ID:** {period.Id}\n" +
                   $"**Referência:** {GetMonthName(period.ReferenceMonth)}/{period.ReferenceYear}\n" +
                   $"**Status:** {GetStatusText(period.Status)}{statusInfo}\n" +
                   $"**Total de Funcionários:** {period.Results.Count}\n\n" +
                   $"**💰 Resumo Financeiro:**\n" +
                   $"  • Total Bruto: R$ {totalBruto:N2}\n" +
                   $"  • Total Descontos: R$ {totalDescontos:N2}\n" +
                   $"  • Total Proventos: R$ {totalProventos:N2}\n" +
                   $"  • **Total Líquido: R$ {totalLiquido:N2}**\n\n" +
                   $"**Observações:** {period.Notes ?? "Nenhuma"}";
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao buscar detalhes do período: {ex.Message}";
        }
    }

    [KernelFunction, Description("Obtém o resumo da folha de pagamento do período atual ou mais recente")]
    public async Task<string> GetCurrentPayrollSummary()
    {
        try
        {
            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;

            // Busca períodos do ano atual
            var periods = await _payrollService.GetPeriodsAsync(currentYear, null);
            
            // Tenta encontrar o período atual ou o mais recente
            var period = periods.FirstOrDefault(p => p.ReferenceMonth == currentMonth && p.ReferenceYear == currentYear)
                      ?? periods.FirstOrDefault();

            if (period == null)
            {
                return $"📋 Não há períodos de folha de pagamento cadastrados para {currentYear}.";
            }

            var totalBruto = period.Results.Sum(r => r.GrossAmount);
            var totalLiquido = period.Results.Sum(r => r.NetAmount);
            var totalDescontos = period.Results.Sum(r => r.TotalDeductions);

            return $"📋 **Resumo da Folha - {GetMonthName(period.ReferenceMonth)}/{period.ReferenceYear}:**\n\n" +
                   $"**Status:** {GetStatusText(period.Status)}\n" +
                   $"**Funcionários:** {period.Results.Count}\n\n" +
                   $"**💰 Totais:**\n" +
                   $"  • Bruto: R$ {totalBruto:N2}\n" +
                   $"  • Descontos: R$ {totalDescontos:N2}\n" +
                   $"  • **Líquido: R$ {totalLiquido:N2}**";
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao buscar resumo da folha: {ex.Message}";
        }
    }

    [KernelFunction, Description("Lista os funcionários e seus valores em um período específico de folha")]
    public async Task<string> ListPayrollEmployees(
        [Description("ID do período de folha de pagamento")] int periodId,
        [Description("Número máximo de funcionários a exibir")] int maxResults = 15)
    {
        try
        {
            var period = await _payrollService.GetPeriodAsync(periodId);

            if (period == null)
            {
                return $"🔍 Período de folha com ID {periodId} não encontrado.";
            }

            if (!period.Results.Any())
            {
                return $"📋 O período {GetMonthName(period.ReferenceMonth)}/{period.ReferenceYear} não possui resultados calculados.";
            }

            var employeeList = period.Results
                .OrderByDescending(r => r.NetAmount)
                .Take(maxResults)
                .Select(r =>
                    $"- **{(string.IsNullOrEmpty(r.EmployeeNameSnapshot) ? $"Funcionário #{r.EmployeeId}" : r.EmployeeNameSnapshot)}** - Bruto: R$ {r.GrossAmount:N2} → Líquido: R$ {r.NetAmount:N2}"
                );

            return $"👥 **Funcionários na Folha de {GetMonthName(period.ReferenceMonth)}/{period.ReferenceYear}:**\n" +
                   $"(Exibindo {Math.Min(maxResults, period.Results.Count)} de {period.Results.Count})\n\n" +
                   string.Join("\n", employeeList);
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao listar funcionários da folha: {ex.Message}";
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
                return $"📊 Não há dados de folha de pagamento para o ano {targetYear}.";
            }

            var totalBrutoAnual = periods.Sum(p => p.Results.Sum(r => r.GrossAmount));
            var totalLiquidoAnual = periods.Sum(p => p.Results.Sum(r => r.NetAmount));
            var totalDescontosAnual = periods.Sum(p => p.Results.Sum(r => r.TotalDeductions));
            var mediaFuncionarios = periods.Any() ? periods.Average(p => p.Results.Count) : 0;

            var periodosCalculados = periods.Count(p => p.Status >= PayrollPeriodStatus.Calculated);
            var periodosAprovados = periods.Count(p => p.Status >= PayrollPeriodStatus.Approved);
            var periodosPagos = periods.Count(p => p.Status == PayrollPeriodStatus.Paid);

            return $"📊 **Estatísticas de Folha de Pagamento - {targetYear}:**\n\n" +
                   $"**Períodos:**\n" +
                   $"  • Total: {periods.Count}\n" +
                   $"  • Calculados: {periodosCalculados}\n" +
                   $"  • Aprovados: {periodosAprovados}\n" +
                   $"  • Pagos: {periodosPagos}\n\n" +
                   $"**💰 Totais Anuais:**\n" +
                   $"  • Bruto: R$ {totalBrutoAnual:N2}\n" +
                   $"  • Descontos: R$ {totalDescontosAnual:N2}\n" +
                   $"  • **Líquido: R$ {totalLiquidoAnual:N2}**\n\n" +
                   $"**📈 Médias:**\n" +
                   $"  • Funcionários/mês: {mediaFuncionarios:F1}\n" +
                   $"  • Folha mensal média: R$ {(periods.Count > 0 ? totalLiquidoAnual / periods.Count : 0):N2}";
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

            var result = $"⏳ **Períodos Pendentes de {currentYear}:**\n\n";

            if (draft.Any())
            {
                result += $"**📝 Aguardando cálculo ({draft.Count}):**\n";
                result += string.Join("\n", draft.Select(p => $"  - {GetMonthName(p.ReferenceMonth)}/{p.ReferenceYear} (ID: {p.Id})"));
                result += "\n\n";
            }

            if (calculated.Any())
            {
                result += $"**✅ Aguardando aprovação ({calculated.Count}):**\n";
                result += string.Join("\n", calculated.Select(p => $"  - {GetMonthName(p.ReferenceMonth)}/{p.ReferenceYear} (ID: {p.Id})"));
                result += "\n\n";
            }

            if (approved.Any())
            {
                result += $"**💳 Aguardando pagamento ({approved.Count}):**\n";
                result += string.Join("\n", approved.Select(p => $"  - {GetMonthName(p.ReferenceMonth)}/{p.ReferenceYear} (ID: {p.Id})"));
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
