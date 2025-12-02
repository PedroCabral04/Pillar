using System.ComponentModel;
using Microsoft.SemanticKernel;
using erp.Services.Financial;
using erp.DTOs.Financial;

namespace erp.Services.Chatbot.ChatbotPlugins;

/// <summary>
/// Plugin para gerenciar fornecedores através do chatbot
/// </summary>
public class SuppliersPlugin
{
    private readonly ISupplierService _supplierService;

    public SuppliersPlugin(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    [KernelFunction, Description("Busca fornecedores pelo nome ou CNPJ/CPF")]
    public async Task<string> SearchSuppliers(
        [Description("Termo de busca: nome ou documento (CNPJ/CPF) do fornecedor")] string searchTerm,
        [Description("Número máximo de resultados a retornar")] int maxResults = 10)
    {
        try
        {
            var (suppliers, total) = await _supplierService.GetPagedAsync(
                page: 1, 
                pageSize: maxResults, 
                search: searchTerm, 
                activeOnly: null);

            if (!suppliers.Any())
            {
                return $"🔍 Nenhum fornecedor encontrado com o termo '{searchTerm}'.";
            }

            var supplierList = suppliers.Select(s =>
                $"- **{s.TradeName ?? s.Name}** (CNPJ: {FormatDocument(s.TaxId)}) - {(s.IsActive ? "✅ Ativo" : "❌ Inativo")}"
            );

            return $"🏢 **Fornecedores encontrados ({total} total):**\n{string.Join("\n", supplierList)}";
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao buscar fornecedores: {ex.Message}";
        }
    }

    [KernelFunction, Description("Obtém detalhes completos de um fornecedor pelo ID")]
    public async Task<string> GetSupplierDetails(
        [Description("ID do fornecedor")] int supplierId)
    {
        try
        {
            var supplier = await _supplierService.GetByIdAsync(supplierId);

            if (supplier == null)
            {
                return $"🔍 Fornecedor com ID {supplierId} não encontrado.";
            }

            return FormatSupplierDetails(supplier);
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao buscar detalhes do fornecedor: {ex.Message}";
        }
    }

    [KernelFunction, Description("Lista todos os fornecedores cadastrados")]
    public async Task<string> ListSuppliers(
        [Description("Número máximo de fornecedores a retornar")] int maxResults = 20,
        [Description("Filtrar apenas fornecedores ativos?")] bool activeOnly = true)
    {
        try
        {
            var (suppliers, total) = await _supplierService.GetPagedAsync(
                page: 1, 
                pageSize: maxResults, 
                search: null, 
                activeOnly: activeOnly);

            if (!suppliers.Any())
            {
                return "🏢 Não há fornecedores cadastrados no momento.";
            }

            var supplierList = suppliers.Select(s =>
                $"- **{s.TradeName ?? s.Name}** (ID: {s.Id}) - CNPJ: {FormatDocument(s.TaxId)} - {(s.IsActive ? "✅ Ativo" : "❌ Inativo")}"
            );

            var statusText = activeOnly ? " ativos" : "";
            return $"🏢 **Fornecedores{statusText} ({total} total):**\n{string.Join("\n", supplierList)}";
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao listar fornecedores: {ex.Message}";
        }
    }

    [KernelFunction, Description("Consulta dados de uma empresa pelo CNPJ na Receita Federal (ReceitaWS)")]
    public async Task<string> LookupCompanyByCNPJ(
        [Description("CNPJ da empresa a ser consultada")] string cnpj)
    {
        try
        {
            // Remove formatação do CNPJ
            var cleanCnpj = cnpj.Replace(".", "").Replace("-", "").Replace("/", "").Trim();

            if (cleanCnpj.Length != 14)
            {
                return "⚠️ CNPJ inválido. O CNPJ deve conter 14 dígitos.";
            }

            var companyData = await _supplierService.GetCompanyDataAsync(cleanCnpj);

            if (companyData == null)
            {
                return $"🔍 Não foi possível consultar os dados do CNPJ {FormatDocument(cleanCnpj)}. Verifique se o CNPJ está correto.";
            }

            if (!string.IsNullOrEmpty(companyData.Status) && companyData.Status.ToUpper() == "ERROR")
            {
                return $"⚠️ Erro na consulta: {companyData.Message ?? "CNPJ não encontrado na base da Receita Federal."}";
            }

            var situacao = companyData.Situacao?.ToUpper() switch
            {
                "ATIVA" => "✅ Ativa",
                "BAIXADA" => "❌ Baixada",
                "INAPTA" => "⚠️ Inapta",
                "SUSPENSA" => "⚠️ Suspensa",
                _ => companyData.Situacao ?? "Não informada"
            };

            var endereco = string.Join(", ", new[]
            {
                companyData.Logradouro,
                companyData.Numero,
                companyData.Complemento,
                companyData.Bairro,
                companyData.Municipio,
                companyData.Uf,
                !string.IsNullOrEmpty(companyData.Cep) ? $"CEP: {FormatCep(companyData.Cep)}" : null
            }.Where(s => !string.IsNullOrEmpty(s)));

            return $"🏢 **Dados da Empresa (Receita Federal):**\n\n" +
                   $"**CNPJ:** {FormatDocument(cleanCnpj)}\n" +
                   $"**Razão Social:** {companyData.Nome ?? "Não informado"}\n" +
                   $"**Nome Fantasia:** {companyData.Fantasia ?? "Não informado"}\n" +
                   $"**Situação:** {situacao}\n" +
                   $"**Natureza Jurídica:** {companyData.NaturezaJuridica ?? "Não informada"}\n" +
                   $"**Endereço:** {(string.IsNullOrEmpty(endereco) ? "Não informado" : endereco)}\n" +
                   $"**Email:** {companyData.Email ?? "Não informado"}\n" +
                   $"**Telefone:** {companyData.Telefone ?? "Não informado"}\n" +
                   $"**Capital Social:** {(companyData.CapitalSocial != null ? $"R$ {decimal.Parse(companyData.CapitalSocial):N2}" : "Não informado")}\n" +
                   $"**Data de Abertura:** {companyData.Abertura ?? "Não informada"}";
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao consultar CNPJ: {ex.Message}";
        }
    }

    [KernelFunction, Description("Consulta endereço pelo CEP (ViaCEP)")]
    public async Task<string> LookupAddressByCEP(
        [Description("CEP a ser consultado")] string cep)
    {
        try
        {
            // Remove formatação do CEP
            var cleanCep = cep.Replace("-", "").Replace(".", "").Trim();

            if (cleanCep.Length != 8)
            {
                return "⚠️ CEP inválido. O CEP deve conter 8 dígitos.";
            }

            var address = await _supplierService.GetAddressAsync(cleanCep);

            if (address == null || address.Erro)
            {
                return $"🔍 CEP {FormatCep(cleanCep)} não encontrado.";
            }

            return $"📍 **Endereço encontrado:**\n\n" +
                   $"**CEP:** {FormatCep(cleanCep)}\n" +
                   $"**Logradouro:** {address.Logradouro ?? "Não informado"}\n" +
                   $"**Complemento:** {address.Complemento ?? "Não informado"}\n" +
                   $"**Bairro:** {address.Bairro ?? "Não informado"}\n" +
                   $"**Cidade:** {address.Localidade ?? "Não informado"}\n" +
                   $"**Estado:** {address.Uf ?? "Não informado"}\n" +
                   $"**IBGE:** {address.Ibge ?? "Não informado"}\n" +
                   $"**DDD:** {address.Ddd ?? "Não informado"}";
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao consultar CEP: {ex.Message}";
        }
    }

    [KernelFunction, Description("Cadastra um novo fornecedor no sistema")]
    public async Task<string> CreateSupplier(
        [Description("Razão social do fornecedor")] string name,
        [Description("CNPJ ou CPF do fornecedor")] string taxId,
        [Description("Nome fantasia (opcional)")] string? tradeName = null,
        [Description("Email do fornecedor (opcional)")] string? email = null,
        [Description("Telefone do fornecedor (opcional)")] string? phone = null,
        [Description("Endereço do fornecedor (opcional)")] string? address = null,
        [Description("Cidade (opcional)")] string? city = null,
        [Description("Estado/UF (opcional)")] string? state = null,
        [Description("CEP (opcional)")] string? zipCode = null)
    {
        try
        {
            var createDto = new CreateSupplierDto
            {
                Name = name,
                TradeName = tradeName,
                TaxId = taxId,
                Email = email,
                Phone = phone,
                Street = address,
                City = city,
                State = state,
                ZipCode = zipCode?.Replace("-", ""),
                IsActive = true
            };

            var supplier = await _supplierService.CreateAsync(createDto, currentUserId: 1); // TODO: obter userId do contexto

            return $"✅ **Fornecedor cadastrado com sucesso!**\n\n" +
                   $"**ID:** {supplier.Id}\n" +
                   $"**Razão Social:** {supplier.Name}\n" +
                   $"**Nome Fantasia:** {supplier.TradeName ?? "Não informado"}\n" +
                   $"**CNPJ/CPF:** {FormatDocument(supplier.TaxId)}\n" +
                   $"**Email:** {supplier.Email ?? "Não informado"}\n" +
                   $"**Telefone:** {supplier.Phone ?? "Não informado"}";
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Já existe"))
        {
            return $"⚠️ {ex.Message}";
        }
        catch (ArgumentException ex)
        {
            return $"⚠️ {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"❌ Erro ao cadastrar fornecedor: {ex.Message}";
        }
    }

    private static string FormatSupplierDetails(SupplierDto supplier)
    {
        var addressParts = new List<string>();
        if (!string.IsNullOrEmpty(supplier.Street)) addressParts.Add(supplier.Street);
        if (!string.IsNullOrEmpty(supplier.Number)) addressParts.Add($"Nº {supplier.Number}");
        if (!string.IsNullOrEmpty(supplier.Complement)) addressParts.Add(supplier.Complement);
        if (!string.IsNullOrEmpty(supplier.District)) addressParts.Add(supplier.District);
        if (!string.IsNullOrEmpty(supplier.City)) addressParts.Add(supplier.City);
        if (!string.IsNullOrEmpty(supplier.State)) addressParts.Add(supplier.State);
        if (!string.IsNullOrEmpty(supplier.ZipCode)) addressParts.Add($"CEP: {FormatCep(supplier.ZipCode)}");

        var fullAddress = addressParts.Any() ? string.Join(", ", addressParts) : "Não informado";

        return $"🏢 **Detalhes do Fornecedor:**\n\n" +
               $"**ID:** {supplier.Id}\n" +
               $"**Razão Social:** {supplier.Name}\n" +
               $"**Nome Fantasia:** {supplier.TradeName ?? "Não informado"}\n" +
               $"**CNPJ/CPF:** {FormatDocument(supplier.TaxId)}\n" +
               $"**Email:** {supplier.Email ?? "Não informado"}\n" +
               $"**Telefone:** {supplier.Phone ?? "Não informado"}\n" +
               $"**Endereço:** {fullAddress}\n" +
               $"**Website:** {supplier.Website ?? "Não informado"}\n" +
               $"**Status:** {(supplier.IsActive ? "✅ Ativo" : "❌ Inativo")}\n" +
               $"**Observações:** {supplier.Notes ?? "Nenhuma"}";
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
