using System.ComponentModel;
using Microsoft.SemanticKernel;
using erp.Services.Sales;
using erp.Services.Financial.Validation;
using erp.DTOs.Sales;
using erp.Models.Sales;

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

    [KernelFunction, Description("Cadastra um novo cliente no sistema. Campos obrigatórios: nome e documento (CPF ou CNPJ). Campos opcionais: nome fantasia, email, telefone, celular, endereço, número, bairro, cidade, estado, CEP, tipo (PF ou PJ).")]
    public async Task<string> CreateCustomer(
        [Description("Nome completo (pessoa física) ou razão social (pessoa jurídica) - obrigatório")] string name,
        [Description("Documento do cliente: CPF (11 dígitos) ou CNPJ (14 dígitos) - obrigatório")] string document,
        [Description("Nome fantasia (opcional, mais usado para PJ)")] string? tradeName = null,
        [Description("Email do cliente (opcional)")] string? email = null,
        [Description("Telefone fixo do cliente (opcional)")] string? phone = null,
        [Description("Celular do cliente (opcional)")] string? mobile = null,
        [Description("Logradouro/Rua (opcional)")] string? street = null,
        [Description("Número do endereço (opcional)")] string? number = null,
        [Description("Bairro (opcional)")] string? neighborhood = null,
        [Description("Cidade do cliente (opcional)")] string? city = null,
        [Description("Estado/UF do cliente (opcional, ex: SP, RJ, MG)")] string? state = null,
        [Description("CEP do cliente (opcional)")] string? zipCode = null)
    {
        try
        {
            // Remove formatação do documento
            var cleanDocument = BrazilianDocumentValidator.RemoveFormatting(document);

            // Validar documento
            if (!BrazilianDocumentValidator.IsValidDocument(cleanDocument))
            {
                var docType = cleanDocument.Length <= 11 ? "CPF" : "CNPJ";
                return $"❌ **{docType} inválido!**\n\nO documento informado não passou na validação. Verifique se os dígitos estão corretos.";
            }

            // Determinar tipo de cliente baseado no documento
            var customerType = cleanDocument.Length == 11 ? CustomerType.Individual : CustomerType.Business;
            var docTypeLabel = cleanDocument.Length == 11 ? "CPF" : "CNPJ";

            // Montar endereço completo se houver logradouro
            string? fullAddress = null;
            if (!string.IsNullOrWhiteSpace(street))
            {
                var addressParts = new List<string> { street };
                if (!string.IsNullOrWhiteSpace(number)) addressParts.Add($"nº {number}");
                fullAddress = string.Join(", ", addressParts);
            }

            var createDto = new CreateCustomerDto
            {
                Name = name,
                TradeName = tradeName,
                Document = cleanDocument,
                Email = email,
                Phone = phone,
                Mobile = mobile,
                Address = fullAddress,
                Neighborhood = neighborhood,
                City = city,
                State = state?.ToUpperInvariant(),
                ZipCode = zipCode?.Replace("-", ""),
                Type = customerType
            };

            var customer = await _customerService.CreateAsync(createDto);

            var addressDisplay = string.Join(", ", new[] { fullAddress, neighborhood, city, state }.Where(s => !string.IsNullOrEmpty(s)));

            return $"""
                ✅ **Cliente Cadastrado!**
                
                | Campo | Valor |
                |-------|-------|
                | **ID** | {customer.Id} |
                | **Tipo** | {(customerType == CustomerType.Individual ? "👤 Pessoa Física" : "🏢 Pessoa Jurídica")} |
                | **Nome** | {customer.Name} |
                | **{docTypeLabel}** | {FormatDocument(customer.Document)} |
                | **Email** | {customer.Email ?? "—"} |
                | **Telefone** | {customer.Phone ?? "—"} |
                | **Celular** | {customer.Mobile ?? "—"} |
                | **Endereço** | {(string.IsNullOrEmpty(addressDisplay) ? "—" : addressDisplay)} |
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

    [KernelFunction, Description("Lista os clientes cadastrados no sistema. Use página > 1 para ver mais.")]
    public async Task<string> ListRecentCustomers(
        [Description("Número máximo de clientes a retornar por página")] int maxResults = 10,
        [Description("Número da página (1 = primeira, 2 = próxima, etc)")] int page = 1,
        [Description("Filtrar apenas clientes ativos? (true/false/null para todos)")] bool? activeOnly = null)
    {
        try
        {
            var (customers, total) = await _customerService.SearchAsync(
                search: null, 
                isActive: activeOnly, 
                page: page, 
                pageSize: maxResults);

            if (!customers.Any() && page == 1)
            {
                return "👥 Não há clientes cadastrados.";
            }
            
            if (!customers.Any())
            {
                return $"👥 Não há mais clientes. Total: {total} clientes.";
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
            
            var shown = (page - 1) * maxResults + customers.Count();
            var remaining = total - shown;
            
            var pageInfo = page > 1 ? $" (Página {page})" : "";
            var moreText = remaining > 0 
                ? $"\n\n*Exibindo {shown} de {total}. Peça \"listar clientes página {page + 1}\" para ver mais.*" 
                : "";

            return $"""
                👥 **Clientes{statusFilter}**{pageInfo} ({total} total)
                
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
