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
        return @"🤖 Sou o assistente virtual do Pillar ERP! Posso ajudar você com:

📦 **Produtos:**
- Listar todos os produtos
- Buscar produtos por nome ou SKU
- Cadastrar novos produtos
- Verificar estoque de produtos

💰 **Vendas:**
- Listar vendas recentes
- Criar novas vendas
- Consultar detalhes de vendas
- Calcular totais de vendas por período

📊 **Exemplos de comandos:**
- ""Mostrar todos os produtos""
- ""Buscar produto notebook""
- ""Cadastrar produto chamado Mouse sem fio, SKU MOUSE001, preço 59.90""
- ""Criar venda para João Silva, email joao@email.com, produto MOUSE001, quantidade 2""
- ""Quanto tenho em estoque do produto MOUSE001?""
- ""Mostrar as últimas 5 vendas""
- ""Qual o total de vendas entre 2025-01-01 e 2025-01-31?""

💡 Dica: Use linguagem natural! Entendo suas solicitações de forma intuitiva.";
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
        return @"📋 **Pillar ERP**

Sistema de gestão empresarial modular e integrado.

**Módulos disponíveis:**
- ✅ Dashboard Analytics
- ✅ Administração de Usuários
- ✅ Gestão de Produtos
- ✅ Controle de Estoque
- ✅ Gestão de Vendas
- ✅ Kanban Pessoal
- ✅ Preferências do Usuário

**Tecnologias:**
- Blazor Server (.NET 9)
- MudBlazor UI
- PostgreSQL
- ASP.NET Core Identity
- Semantic Kernel (IA)

Desenvolvido com ❤️ para simplificar a gestão empresarial.";
    }
}
