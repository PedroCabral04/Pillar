using System.ComponentModel;
using Microsoft.SemanticKernel;
using erp.Services.Financial;
using erp.Models.Financial;

namespace erp.Services.Chatbot.ChatbotPlugins;

public class FinancialPlugin
{
    private readonly IAccountPayableService _payableService;
    private readonly IAccountReceivableService _receivableService;

    public FinancialPlugin(
        IAccountPayableService payableService,
        IAccountReceivableService receivableService)
    {
        _payableService = payableService;
        _receivableService = receivableService;
    }

    [KernelFunction, Description("Verifica se existem contas a pagar pendentes ou vencidas")]
    public async Task<string> GetPendingPayables()
    {
        var overdue = await _payableService.GetOverdueAsync();
        var dueSoon = await _payableService.GetDueSoonAsync(7);
        
        if (!overdue.Any() && !dueSoon.Any())
            return "Não há contas a pagar vencidas ou vencendo nos próximos 7 dias.";

        var sb = new System.Text.StringBuilder();
        
        if (overdue.Any())
        {
            sb.AppendLine($"🚨 **{overdue.Count} Contas Vencidas:**");
            foreach (var item in overdue.Take(5))
            {
                sb.AppendLine($"- Nota {item.InvoiceNumber ?? "N/A"} ({item.SupplierName}): {item.OriginalAmount:C} (Venceu: {item.DueDate:dd/MM})");
            }
            if (overdue.Count > 5) sb.AppendLine($"+ {overdue.Count - 5} outras...");
            sb.AppendLine();
        }

        if (dueSoon.Any())
        {
            sb.AppendLine($"📅 **{dueSoon.Count} Contas Vencendo em Breve:**");
            foreach (var item in dueSoon.Take(5))
            {
                sb.AppendLine($"- Nota {item.InvoiceNumber ?? "N/A"} ({item.SupplierName}): {item.OriginalAmount:C} (Vence: {item.DueDate:dd/MM})");
            }
        }

        return sb.ToString();
    }

    [KernelFunction, Description("Verifica se existem contas a receber atrasadas")]
    public async Task<string> GetOverdueReceivables()
    {
        var overdue = await _receivableService.GetOverdueAsync();
        
        if (!overdue.Any())
            return "Não há contas a receber em atraso.";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"💰 **{overdue.Count} Recebimentos em Atraso:**");
        
        foreach (var item in overdue.Take(5))
        {
            sb.AppendLine($"- Nota {item.InvoiceNumber ?? "N/A"} ({item.CustomerName}): {item.OriginalAmount:C} (Venceu: {item.DueDate:dd/MM})");
        }
        
        if (overdue.Count > 5) sb.AppendLine($"+ {overdue.Count - 5} outros...");

        return sb.ToString();
    }

    [KernelFunction, Description("Obtém um resumo do fluxo de caixa atual (contas a pagar vs receber)")]
    public async Task<string> GetCashFlowSummary()
    {
        var payablesPending = await _payableService.GetTotalByStatusAsync(AccountStatus.Pending);
        var payablesOverdue = await _payableService.GetTotalByStatusAsync(AccountStatus.Overdue);
        
        var receivablesPending = await _receivableService.GetTotalByStatusAsync(AccountStatus.Pending);
        var receivablesOverdue = await _receivableService.GetTotalByStatusAsync(AccountStatus.Overdue);

        var totalPayables = payablesPending + payablesOverdue;
        var totalReceivables = receivablesPending + receivablesOverdue;
        var balance = totalReceivables - totalPayables;

        return $@"📊 **Resumo Financeiro**

🔴 **A Pagar:** {totalPayables:C}
   - Pendente: {payablesPending:C}
   - Vencido: {payablesOverdue:C}

🟢 **A Receber:** {totalReceivables:C}
   - Pendente: {receivablesPending:C}
   - Vencido: {receivablesOverdue:C}

💵 **Saldo Previsto:** {balance:C}";
    }
}
