using System.Text;
using System.Text.Json;

namespace erp.Services;

public interface IApiService
{
    Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> GetAsync(string endpoint, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> PostAsJsonAsync<T>(string endpoint, T data, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> PutAsJsonAsync<T>(string endpoint, T data, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> DeleteAsync(string endpoint, CancellationToken cancellationToken = default);
}

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    private HttpRequestMessage CreateRequestWithCookies(HttpMethod method, string endpoint)
    {
        var request = new HttpRequestMessage(method, endpoint);
        
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            // Forward all cookies from the current HTTP context
            foreach (var cookie in httpContext.Request.Cookies)
            {
                request.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
            }
        }
        
        return request;
    }

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        var response = await GetAsync(endpoint, cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        
        return default;
    }

    public async Task<HttpResponseMessage> GetAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        var request = CreateRequestWithCookies(HttpMethod.Get, endpoint);
        return await _httpClient.SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> PostAsJsonAsync<T>(string endpoint, T data, CancellationToken cancellationToken = default)
    {
        var request = CreateRequestWithCookies(HttpMethod.Post, endpoint);
        var json = JsonSerializer.Serialize(data);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _httpClient.SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> PutAsJsonAsync<T>(string endpoint, T data, CancellationToken cancellationToken = default)
    {
        var request = CreateRequestWithCookies(HttpMethod.Put, endpoint);
        var json = JsonSerializer.Serialize(data);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _httpClient.SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        var request = CreateRequestWithCookies(HttpMethod.Delete, endpoint);
        return await _httpClient.SendAsync(request, cancellationToken);
    }
}
