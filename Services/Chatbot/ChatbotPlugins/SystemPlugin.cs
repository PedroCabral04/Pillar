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

📦 **Produtos & Estoque:**
- Listar e buscar produtos
- Cadastrar novos produtos
- Verificar níveis de estoque

💰 **Vendas:**
- Listar vendas recentes
- Criar novas vendas
- Consultar detalhes e totais por período

👥 **Clientes:**
- Buscar clientes por nome, CPF/CNPJ ou email
- Cadastrar novos clientes
- Consultar histórico de clientes

🏢 **Fornecedores:**
- Buscar e listar fornecedores
- Cadastrar novos fornecedores
- Consultar CNPJ na Receita Federal
- Consultar endereço por CEP

📊 **Financeiro:**
- Contas a pagar e receber
- Resumo de fluxo de caixa
- Contas em atraso

🖥️ **Ativos (Patrimônio):**
- Listar todos os ativos da empresa
- Buscar ativos por código ou nome
- Ver ativos atribuídos a funcionários
- Manutenções agendadas e em atraso
- Estatísticas do patrimônio

💼 **Folha de Pagamento:**
- Listar períodos de folha
- Resumo mensal e anual
- Períodos pendentes (cálculo, aprovação, pagamento)

👔 **Recursos Humanos:**
- Buscar funcionários
- Listar membros por departamento

💡 **Exemplos de comandos:**
- ""Listar todos os ativos""
- ""Buscar cliente João Silva""
- ""Consultar CNPJ 12.345.678/0001-00""
- ""Mostrar folha de pagamento de 2024""
- ""Quais manutenções estão em atraso?""
- ""Cadastrar fornecedor XYZ LTDA, CNPJ 12345678000100""

Use linguagem natural! Entendo suas solicitações de forma intuitiva.";
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
- ✅ Gestão de Clientes
- ✅ Gestão de Fornecedores
- ✅ Contas a Pagar/Receber
- ✅ Gestão de Ativos (Patrimônio)
- ✅ Folha de Pagamento
- ✅ Recursos Humanos
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
