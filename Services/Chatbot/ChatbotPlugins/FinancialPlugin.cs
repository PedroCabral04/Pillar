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
            return "✅ Não há contas a pagar vencidas ou vencendo nos próximos 7 dias.";

        var result = "💳 **Contas a Pagar**\n\n";
        
        if (overdue.Any())
        {
            var items = overdue.Take(5).Select(i => 
                $"| {i.InvoiceNumber ?? "N/A"} | {i.SupplierName} | R$ {i.OriginalAmount:N2} | {i.DueDate:dd/MM} |"
            );
            var moreText = overdue.Count > 5 ? $"\n\n*...e mais {overdue.Count - 5} contas vencidas.*" : "";
            
            result += $"""
                🚨 **Vencidas** ({overdue.Count})
                
                | Nota | Fornecedor | Valor | Venceu |
                |------|------------|-------|--------|
                {string.Join("\n", items)}{moreText}
                
                """;
        }

        if (dueSoon.Any())
        {
            var items = dueSoon.Take(5).Select(i => 
                $"| {i.InvoiceNumber ?? "N/A"} | {i.SupplierName} | R$ {i.OriginalAmount:N2} | {i.DueDate:dd/MM} |"
            );
            
            result += $"""
                📅 **Vencendo em Breve** ({dueSoon.Count})
                
                | Nota | Fornecedor | Valor | Vence |
                |------|------------|-------|-------|
                {string.Join("\n", items)}
                """;
        }

        return result.TrimEnd();
    }

    [KernelFunction, Description("Verifica se existem contas a receber atrasadas")]
    public async Task<string> GetOverdueReceivables()
    {
        var overdue = await _receivableService.GetOverdueAsync();
        
        if (!overdue.Any())
            return "✅ Não há contas a receber em atraso.";

        var items = overdue.Take(5).Select(i => 
            $"| {i.InvoiceNumber ?? "N/A"} | {i.CustomerName} | R$ {i.OriginalAmount:N2} | {i.DueDate:dd/MM} |"
        );
        var moreText = overdue.Count > 5 ? $"\n\n*...e mais {overdue.Count - 5} contas.*" : "";

        return $"""
            💰 **Recebíveis em Atraso** ({overdue.Count})
            
            | Nota | Cliente | Valor | Venceu |
            |------|---------|-------|--------|
            {string.Join("\n", items)}{moreText}
            """;
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
        var balanceIcon = balance >= 0 ? "🟢" : "🔴";

        return $"""
            📊 **Resumo Financeiro**
            
            | Categoria | Pendente | Vencido | Total |
            |-----------|----------|---------|-------|
            | 🔴 **A Pagar** | R$ {payablesPending:N2} | R$ {payablesOverdue:N2} | R$ {totalPayables:N2} |
            | 🟢 **A Receber** | R$ {receivablesPending:N2} | R$ {receivablesOverdue:N2} | R$ {totalReceivables:N2} |
            
            ---
            {balanceIcon} **Saldo Previsto:** R$ {balance:N2}
            """;
    }
}
