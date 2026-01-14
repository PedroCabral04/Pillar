using System.ComponentModel;
using Microsoft.SemanticKernel;
using erp.Services.Financial;
using erp.Models.Financial;
using erp.DTOs.Financial;

namespace erp.Services.Chatbot.ChatbotPlugins;

public class FinancialPlugin
{
    private readonly IAccountPayableService _payableService;
    private readonly IAccountReceivableService _receivableService;
    private readonly IFinancialCategoryService _categoryService;
    private readonly IChatbotCacheService _cacheService;
    private const string PluginName = "FinancialPlugin";

    public FinancialPlugin(
        IAccountPayableService payableService,
        IAccountReceivableService receivableService,
        IFinancialCategoryService categoryService,
        IChatbotCacheService cacheService)
    {
        _payableService = payableService;
        _receivableService = receivableService;
        _categoryService = categoryService;
        _cacheService = cacheService;
    }

    [KernelFunction, Description("Verifica se existem contas a pagar pendentes ou vencidas")]
    public async Task<string> GetPendingPayables()
    {
        // Tentar obter do cache
        var cachedResult = _cacheService.GetPluginData<string>(PluginName, nameof(GetPendingPayables));
        if (cachedResult != null)
        {
            return cachedResult;
        }

        var overdue = await _payableService.GetOverdueAsync();
        var dueSoon = await _payableService.GetDueSoonAsync(7);
        
        if (!overdue.Any() && !dueSoon.Any())
        {
            var emptyResult = "✅ Não há contas a pagar vencidas ou vencendo nos próximos 7 dias.";
            _cacheService.SetPluginData(PluginName, nameof(GetPendingPayables), emptyResult);
            return emptyResult;
        }

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

        var response = result.TrimEnd();
        _cacheService.SetPluginData(PluginName, nameof(GetPendingPayables), response);
        return response;
    }

    [KernelFunction, Description("Verifica se existem contas a receber atrasadas")]
    public async Task<string> GetOverdueReceivables()
    {
        // Tentar obter do cache
        var cachedResult = _cacheService.GetPluginData<string>(PluginName, nameof(GetOverdueReceivables));
        if (cachedResult != null)
        {
            return cachedResult;
        }

        var overdue = await _receivableService.GetOverdueAsync();
        
        if (!overdue.Any())
        {
            var emptyResult = "✅ Não há contas a receber em atraso.";
            _cacheService.SetPluginData(PluginName, nameof(GetOverdueReceivables), emptyResult);
            return emptyResult;
        }

        var items = overdue.Take(5).Select(i => 
            $"| {i.InvoiceNumber ?? "N/A"} | {i.CustomerName} | R$ {i.OriginalAmount:N2} | {i.DueDate:dd/MM} |"
        );
        var moreText = overdue.Count > 5 ? $"\n\n*...e mais {overdue.Count - 5} contas.*" : "";

        var response = $"""
            💰 **Recebíveis em Atraso** ({overdue.Count})
            
            | Nota | Cliente | Valor | Venceu |
            |------|---------|-------|--------|
            {string.Join("\n", items)}{moreText}
            """;
        
        _cacheService.SetPluginData(PluginName, nameof(GetOverdueReceivables), response);
        return response;
    }

    [KernelFunction, Description("Obtém um resumo do fluxo de caixa atual (contas a pagar vs receber)")]
    public async Task<string> GetCashFlowSummary()
    {
        // Tentar obter do cache
        var cachedResult = _cacheService.GetPluginData<string>(PluginName, nameof(GetCashFlowSummary));
        if (cachedResult != null)
        {
            return cachedResult;
        }

        var payablesPending = await _payableService.GetTotalByStatusAsync(AccountStatus.Pending);
        var payablesOverdue = await _payableService.GetTotalByStatusAsync(AccountStatus.Overdue);
        
        var receivablesPending = await _receivableService.GetTotalByStatusAsync(AccountStatus.Pending);
        var receivablesOverdue = await _receivableService.GetTotalByStatusAsync(AccountStatus.Overdue);

        var totalPayables = payablesPending + payablesOverdue;
        var totalReceivables = receivablesPending + receivablesOverdue;
        var balance = totalReceivables - totalPayables;
        var balanceIcon = balance >= 0 ? "🟢" : "🔴";

        var response = $"""
            📊 **Resumo Financeiro**
            
            | Categoria | Pendente | Vencido | Total |
            |-----------|----------|---------|-------|
            | 🔴 **A Pagar** | R$ {payablesPending:N2} | R$ {payablesOverdue:N2} | R$ {totalPayables:N2} |
            | 🟢 **A Receber** | R$ {receivablesPending:N2} | R$ {receivablesOverdue:N2} | R$ {totalReceivables:N2} |
            
            ---
            {balanceIcon} **Saldo Previsto:** R$ {balance:N2}
            """;
        
        _cacheService.SetPluginData(PluginName, nameof(GetCashFlowSummary), response);
        return response;
    }

    [KernelFunction, Description("Lista as categorias financeiras (receitas ou despesas)")]
    public async Task<string> GetFinancialCategories(
        [Description("O tipo de categoria para filtrar: 'receita', 'despesa' ou vazio para todas")] string? type = null)
    {
        var cacheKey = $"{nameof(GetFinancialCategories)}_{type ?? "all"}";
        var cachedResult = _cacheService.GetPluginData<string>(PluginName, cacheKey);
        if (cachedResult != null)
        {
            return cachedResult;
        }

        List<FinancialCategoryDto> categories;

        if (!string.IsNullOrWhiteSpace(type))
        {
            var normalizedType = type.ToLower().Trim();
            if (normalizedType.Contains("receita") || normalizedType.Contains("entrada"))
            {
                categories = await _categoryService.GetByTypeAsync(CategoryType.Revenue);
            }
            else if (normalizedType.Contains("despesa") || normalizedType.Contains("saida") || normalizedType.Contains("saída"))
            {
                categories = await _categoryService.GetByTypeAsync(CategoryType.Expense);
            }
            else
            {
                categories = await _categoryService.GetAllAsync();
            }
        }
        else
        {
            categories = await _categoryService.GetAllAsync();
        }

        if (!categories.Any())
        {
            return "Não encontrei nenhuma categoria financeira cadastrada.";
        }

        var result = "📂 **Categorias Financeiras**\n\n";
        
        // Group by type if showing all
        var revenues = categories.Where(c => c.Type == CategoryType.Revenue).ToList();
        var expenses = categories.Where(c => c.Type == CategoryType.Expense).ToList();

        if (revenues.Any())
        {
            result += "**Receitas:**\n";
            foreach (var cat in revenues)
            {
                result += $"- {cat.Name}\n";
            }
            result += "\n";
        }

        if (expenses.Any())
        {
            result += "**Despesas:**\n";
            foreach (var cat in expenses)
            {
                result += $"- {cat.Name}\n";
            }
        }

        _cacheService.SetPluginData(PluginName, cacheKey, result);
        return result;
    }
}
