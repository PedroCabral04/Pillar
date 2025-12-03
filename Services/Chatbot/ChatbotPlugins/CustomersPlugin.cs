using System.ComponentModel;
using Microsoft.SemanticKernel;
using erp.Services.Sales;
using erp.DTOs.Sales;

namespace erp.Services.Chatbot.ChatbotPlugins;

/// <summary>
/// Plugin para gerenciar clientes através do chatbot
/// </summary>
public class CustomersPlugin
{
    private readonly ICustomerService _customerService;

    public CustomersPlugin(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [KernelFunction, Description("Busca clientes pelo nome, documento (CPF/CNPJ) ou email")]
    public async Task<string> SearchCustomers(
        [Description("Termo de busca: nome, documento (CPF/CNPJ) ou email do cliente")] string searchTerm,
        [Description("Número máximo de resultados a retornar")] int maxResults = 10)
    {
        try
        {
            var (customers, total) = await _customerService.SearchAsync(searchTerm, isActive: null, page: 1, pageSize: maxResults);

            if (!customers.Any())
            {
                return $"🔍 Nenhum cliente encontrado com **'{searchTerm}'**.";
            }

            var list = customers.Select(c =>
                $"| {c.Name} | {FormatDocument(c.Document)} | {(c.IsActive ? "✅" : "❌")} | {c.Email ?? "—"} |"
            );
            
            var remaining = total - maxResults;
            var moreText = remaining > 0 ? $"\n\n*...e mais {remaining} clientes.*" : "";

            return $"""
                👥 **Clientes Encontrados** ({total} total)
                
                | Nome | Documento | Ativo | Email |
                |------|-----------|-------|-------|
                {string.Join("\n", list)}{moreText}
                """;
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao buscar clientes: {ex.Message}";
        }
    }

    [KernelFunction, Description("Obtém detalhes completos de um cliente pelo ID")]
    public async Task<string> GetCustomerDetails(
        [Description("ID do cliente")] int customerId)
    {
        try
        {
            var customer = await _customerService.GetByIdAsync(customerId);

            if (customer == null)
            {
                return $"🔍 Cliente com ID {customerId} não encontrado.";
            }

            return FormatCustomerDetails(customer);
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao buscar detalhes do cliente: {ex.Message}";
        }
    }

    [KernelFunction, Description("Busca um cliente pelo documento (CPF ou CNPJ)")]
    public async Task<string> GetCustomerByDocument(
        [Description("Documento do cliente (CPF ou CNPJ)")] string document)
    {
        try
        {
            // Remove formatação do documento
            var cleanDocument = document.Replace(".", "").Replace("-", "").Replace("/", "").Trim();
            
            var customer = await _customerService.GetByDocumentAsync(cleanDocument);

            if (customer == null)
            {
                return $"🔍 Nenhum cliente encontrado com o documento '{document}'.";
            }

            return FormatCustomerDetails(customer);
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao buscar cliente por documento: {ex.Message}";
        }
    }

    [KernelFunction, Description("Cadastra um novo cliente no sistema")]
    public async Task<string> CreateCustomer(
        [Description("Nome completo ou razão social do cliente")] string name,
        [Description("Documento do cliente (CPF ou CNPJ)")] string document,
        [Description("Email do cliente (opcional)")] string? email = null,
        [Description("Telefone do cliente (opcional)")] string? phone = null,
        [Description("Endereço do cliente (opcional)")] string? address = null,
        [Description("Cidade do cliente (opcional)")] string? city = null,
        [Description("Estado/UF do cliente (opcional)")] string? state = null,
        [Description("CEP do cliente (opcional)")] string? zipCode = null)
    {
        try
        {
            // Remove formatação do documento
            var cleanDocument = document.Replace(".", "").Replace("-", "").Replace("/", "").Trim();

            var createDto = new CreateCustomerDto
            {
                Name = name,
                Document = cleanDocument,
                Email = email,
                Phone = phone,
                Address = address,
                City = city,
                State = state,
                ZipCode = zipCode?.Replace("-", "")
            };

            var customer = await _customerService.CreateAsync(createDto);

            return $"""
                ✅ **Cliente Cadastrado!**
                
                - **ID:** {customer.Id}
                - **Nome:** {customer.Name}
                - **Documento:** {FormatDocument(customer.Document)}
                - **Email:** {customer.Email ?? "—"}
                - **Telefone:** {customer.Phone ?? "—"}
                """;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("já existe"))
        {
            return $"⚠️ {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao cadastrar cliente: {ex.Message}";
        }
    }

    [KernelFunction, Description("Lista os clientes mais recentes cadastrados no sistema")]
    public async Task<string> ListRecentCustomers(
        [Description("Número máximo de clientes a retornar")] int maxResults = 10,
        [Description("Filtrar apenas clientes ativos? (true/false/null para todos)")] bool? activeOnly = null)
    {
        try
        {
            var (customers, total) = await _customerService.SearchAsync(
                search: null, 
                isActive: activeOnly, 
                page: 1, 
                pageSize: maxResults);

            if (!customers.Any())
            {
                return "👥 Não há clientes cadastrados.";
            }

            var list = customers.Select(c =>
                $"| {c.Id} | {c.Name} | {FormatDocument(c.Document)} | {(c.IsActive ? "✅" : "❌")} |"
            );

            var statusFilter = activeOnly switch
            {
                true => " Ativos",
                false => " Inativos",
                _ => ""
            };
            
            var remaining = total - maxResults;
            var moreText = remaining > 0 ? $"\n\n*...e mais {remaining} clientes.*" : "";

            return $"""
                👥 **Clientes{statusFilter}** ({total} total)
                
                | ID | Nome | Documento | Ativo |
                |----|------|-----------|-------|
                {string.Join("\n", list)}{moreText}
                """;
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao listar clientes: {ex.Message}";
        }
    }

    private static string FormatCustomerDetails(CustomerDto customer)
    {
        var addressParts = new List<string>();
        if (!string.IsNullOrEmpty(customer.Address)) addressParts.Add(customer.Address);
        if (!string.IsNullOrEmpty(customer.City)) addressParts.Add(customer.City);
        if (!string.IsNullOrEmpty(customer.State)) addressParts.Add(customer.State);
        if (!string.IsNullOrEmpty(customer.ZipCode)) addressParts.Add($"CEP: {FormatCep(customer.ZipCode)}");

        var fullAddress = addressParts.Any() ? string.Join(", ", addressParts) : "—";

        return $"""
            👤 **Cliente #{customer.Id}**
            
            | Campo | Valor |
            |-------|-------|
            | **Nome** | {customer.Name} |
            | **Documento** | {FormatDocument(customer.Document)} |
            | **Email** | {customer.Email ?? "—"} |
            | **Telefone** | {customer.Phone ?? "—"} |
            | **Endereço** | {fullAddress} |
            | **Status** | {(customer.IsActive ? "✅ Ativo" : "❌ Inativo")} |
            | **Cadastro** | {customer.CreatedAt:dd/MM/yyyy HH:mm} |
            """;
    }

    private static string FormatDocument(string document)
    {
        if (string.IsNullOrEmpty(document)) return "Não informado";
        
        var cleanDoc = document.Replace(".", "").Replace("-", "").Replace("/", "");
        
        return cleanDoc.Length == 11
            ? $"{cleanDoc[..3]}.{cleanDoc[3..6]}.{cleanDoc[6..9]}-{cleanDoc[9..]}" // CPF
            : cleanDoc.Length == 14
                ? $"{cleanDoc[..2]}.{cleanDoc[2..5]}.{cleanDoc[5..8]}/{cleanDoc[8..12]}-{cleanDoc[12..]}" // CNPJ
                : document;
    }

    private static string FormatCep(string cep)
    {
        if (string.IsNullOrEmpty(cep)) return cep;
        var cleanCep = cep.Replace("-", "");
        return cleanCep.Length == 8 ? $"{cleanCep[..5]}-{cleanCep[5..]}" : cep;
    }
}
