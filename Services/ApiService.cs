using System.Text;
using System.Text.Json;

namespace erp.Services;

public interface IApiService
{
    Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> GetAsync(string endpoint, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> PostAsJsonAsync<T>(string endpoint, T data, CancellationToken cancellationToken = default);
    Task<TResponse?> PostAsJsonAsync<TRequest, TResponse>(string endpoint, TRequest data, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> PutAsJsonAsync<T>(string endpoint, T data, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> DeleteAsync(string endpoint, CancellationToken cancellationToken = default);
    
    // Loading state management
    bool IsLoading { get; }
    event EventHandler<bool>? LoadingStateChanged;
}

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private int _activeRequests = 0;
    private readonly object _lock = new();

    public ApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsLoading => _activeRequests > 0;
    public event EventHandler<bool>? LoadingStateChanged;

    private void SetLoading(bool loading)
    {
        bool shouldNotify = false;
        bool newState = false;

        lock (_lock)
        {
            if (loading)
            {
                _activeRequests++;
                if (_activeRequests == 1)
                {
                    shouldNotify = true;
                    newState = true;
                }
            }
            else
            {
                _activeRequests = Math.Max(0, _activeRequests - 1);
                if (_activeRequests == 0)
                {
                    shouldNotify = true;
                    newState = false;
                }
            }
        }

        if (shouldNotify)
        {
            LoadingStateChanged?.Invoke(this, newState);
        }
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
        SetLoading(true);
        try
        {
            var fullUri = new Uri(_httpClient.BaseAddress!, endpoint);
            var uriBuilder = new UriBuilder(fullUri);
            var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
            query["_"] = DateTime.UtcNow.Ticks.ToString();
            uriBuilder.Query = query.ToString();

            var request = CreateRequestWithCookies(HttpMethod.Get, uriBuilder.Uri.PathAndQuery);
            return await _httpClient.SendAsync(request, cancellationToken);
        }
        finally
        {
            SetLoading(false);
        }
    }

    public async Task<HttpResponseMessage> PostAsJsonAsync<T>(string endpoint, T data, CancellationToken cancellationToken = default)
    {
        SetLoading(true);
        try
        {
            var request = CreateRequestWithCookies(HttpMethod.Post, endpoint);
            var json = JsonSerializer.Serialize(data);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return await _httpClient.SendAsync(request, cancellationToken);
        }
        finally
        {
            SetLoading(false);
        }
    }

    public async Task<TResponse?> PostAsJsonAsync<TRequest, TResponse>(string endpoint, TRequest data, CancellationToken cancellationToken = default)
    {
        var response = await PostAsJsonAsync(endpoint, data, cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<TResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        
        return default;
    }

    public async Task<HttpResponseMessage> PutAsJsonAsync<T>(string endpoint, T data, CancellationToken cancellationToken = default)
    {
        SetLoading(true);
        try
        {
            var request = CreateRequestWithCookies(HttpMethod.Put, endpoint);
            var json = JsonSerializer.Serialize(data);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return await _httpClient.SendAsync(request, cancellationToken);
        }
        finally
        {
            SetLoading(false);
        }
    }

    public async Task<HttpResponseMessage> DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        SetLoading(true);
        try
        {
            var request = CreateRequestWithCookies(HttpMethod.Delete, endpoint);
            return await _httpClient.SendAsync(request, cancellationToken);
        }
        finally
        {
            SetLoading(false);
        }
    }
}
