using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace erp.Services.Financial.Validation;

/// <summary>
/// Service for integrating with ViaCEP API
/// </summary>
public interface IViaCepService
{
    Task<ViaCepResponse?> GetAddressByCepAsync(string cep);
}

public class ViaCepService : IViaCepService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ViaCepService> _logger;

    public ViaCepService(HttpClient httpClient, ILogger<ViaCepService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("https://viacep.com.br/");
    }

    public async Task<ViaCepResponse?> GetAddressByCepAsync(string cep)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(cep))
                return null;

            // Remove formatting
            cep = cep.Replace("-", "").Replace(".", "").Trim();

            if (cep.Length != 8)
                return null;

            var response = await _httpClient.GetAsync($"ws/{cep}/json/");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("ViaCEP API returned status {StatusCode} for CEP {Cep}", 
                    response.StatusCode, cep);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<ViaCepResponse>();

            if (result?.Erro == true)
            {
                _logger.LogWarning("CEP {Cep} not found in ViaCEP", cep);
                return null;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ViaCEP API for CEP {Cep}", cep);
            return null;
        }
    }
}

public class ViaCepResponse
{
    [JsonPropertyName("cep")]
    public string? Cep { get; set; }

    [JsonPropertyName("logradouro")]
    public string? Logradouro { get; set; }

    [JsonPropertyName("complemento")]
    public string? Complemento { get; set; }

    [JsonPropertyName("bairro")]
    public string? Bairro { get; set; }

    [JsonPropertyName("localidade")]
    public string? Localidade { get; set; }

    [JsonPropertyName("uf")]
    public string? Uf { get; set; }

    [JsonPropertyName("ibge")]
    public string? Ibge { get; set; }

    [JsonPropertyName("gia")]
    public string? Gia { get; set; }

    [JsonPropertyName("ddd")]
    public string? Ddd { get; set; }

    [JsonPropertyName("siafi")]
    public string? Siafi { get; set; }

    [JsonPropertyName("erro")]
    public bool Erro { get; set; }
}
