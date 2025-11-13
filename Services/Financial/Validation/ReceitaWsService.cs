using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace erp.Services.Financial.Validation;

/// <summary>
/// Service for integrating with ReceitaWS API (CNPJ consultation)
/// </summary>
public interface IReceitaWsService
{
    Task<ReceitaWsResponse?> GetCompanyByCnpjAsync(string cnpj);
}

public class ReceitaWsService : IReceitaWsService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ReceitaWsService> _logger;

    public ReceitaWsService(HttpClient httpClient, ILogger<ReceitaWsService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("https://www.receitaws.com.br/");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Pillar ERP/1.0");
    }

    public async Task<ReceitaWsResponse?> GetCompanyByCnpjAsync(string cnpj)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(cnpj))
                return null;

            // Remove formatting
            cnpj = BrazilianDocumentValidator.RemoveFormatting(cnpj);

            if (!BrazilianDocumentValidator.IsValidCnpj(cnpj))
            {
                _logger.LogWarning("Invalid CNPJ format: {Cnpj}", cnpj);
                return null;
            }

            var response = await _httpClient.GetAsync($"v1/cnpj/{cnpj}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("ReceitaWS API returned status {StatusCode} for CNPJ {Cnpj}", 
                    response.StatusCode, cnpj);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<ReceitaWsResponse>();

            if (result?.Status == "ERROR")
            {
                _logger.LogWarning("CNPJ {Cnpj} not found: {Message}", cnpj, result.Message);
                return null;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ReceitaWS API for CNPJ {Cnpj}", cnpj);
            return null;
        }
    }
}

public class ReceitaWsResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("cnpj")]
    public string? Cnpj { get; set; }

    [JsonPropertyName("nome")]
    public string? Nome { get; set; }

    [JsonPropertyName("fantasia")]
    public string? Fantasia { get; set; }

    [JsonPropertyName("abertura")]
    public string? Abertura { get; set; }

    [JsonPropertyName("situacao")]
    public string? Situacao { get; set; }

    [JsonPropertyName("tipo")]
    public string? Tipo { get; set; }

    [JsonPropertyName("porte")]
    public string? Porte { get; set; }

    [JsonPropertyName("natureza_juridica")]
    public string? NaturezaJuridica { get; set; }

    [JsonPropertyName("logradouro")]
    public string? Logradouro { get; set; }

    [JsonPropertyName("numero")]
    public string? Numero { get; set; }

    [JsonPropertyName("complemento")]
    public string? Complemento { get; set; }

    [JsonPropertyName("bairro")]
    public string? Bairro { get; set; }

    [JsonPropertyName("municipio")]
    public string? Municipio { get; set; }

    [JsonPropertyName("uf")]
    public string? Uf { get; set; }

    [JsonPropertyName("cep")]
    public string? Cep { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("telefone")]
    public string? Telefone { get; set; }

    [JsonPropertyName("capital_social")]
    public string? CapitalSocial { get; set; }
}
