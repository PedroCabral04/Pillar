using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace erp.Services.Chatbot.ChatbotPlugins;

/// <summary>
/// Plugin para informações e ajuda do sistema
/// </summary>
public class SystemPlugin
{
    [KernelFunction, Description("Fornece informações sobre o que o assistente pode fazer")]
    public string GetHelp()
    {
        return """
            🤖 **Assistente Pillar ERP**
            
            Posso ajudar você com os seguintes módulos:
            
            | Módulo | Funcionalidades |
            |--------|-----------------|
            | 📦 **Produtos** | Listar, buscar, cadastrar, verificar estoque |
            | 🛒 **Vendas** | Listar recentes, criar vendas, consultar totais |
            | 👥 **Clientes** | Buscar, cadastrar, consultar histórico |
            | 🏢 **Fornecedores** | Buscar, cadastrar, consultar CNPJ/CEP |
            | 💳 **Financeiro** | Contas a pagar/receber, fluxo de caixa |
            | 🖥️ **Ativos** | Listar, buscar, manutenções, estatísticas |
            | 💼 **Folha** | Períodos, resumos mensais/anuais |
            | 👔 **RH** | Buscar funcionários, listar departamentos |
            
            ---
            
            💡 **Exemplos de uso:**
            - *"Listar produtos"*
            - *"Buscar cliente João"*
            - *"Resumo financeiro"*
            - *"Manutenções em atraso"*
            
            Use linguagem natural!
            """;
    }

    [KernelFunction, Description("Retorna a data e hora atual")]
    public string GetCurrentDateTime()
    {
        var now = DateTime.Now;
        return $"Data e hora atual: {now:dd/MM/yyyy HH:mm:ss}";
    }

    [KernelFunction, Description("Fornece informações sobre o sistema Pillar ERP")]
    public string GetSystemInfo()
    {
        return """
            📋 **Pillar ERP**
            
            Sistema de gestão empresarial modular e integrado.
            
            | Módulo | Status |
            |--------|--------|
            | Dashboard | ✅ |
            | Usuários | ✅ |
            | Produtos | ✅ |
            | Estoque | ✅ |
            | Vendas | ✅ |
            | Clientes | ✅ |
            | Fornecedores | ✅ |
            | Financeiro | ✅ |
            | Ativos | ✅ |
            | Folha | ✅ |
            | RH | ✅ |
            
            ---
            
            🛠️ **Tecnologias:** Blazor Server, .NET 9, MudBlazor, PostgreSQL, Semantic Kernel
            """;
    }
}
