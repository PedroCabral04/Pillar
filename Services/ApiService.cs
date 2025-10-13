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
    
    event EventHandler<bool>? LoadingStateChanged;
    event EventHandler<string>? ErrorOccurred;
    bool IsLoading { get; }
    void SetTimeout(TimeSpan timeout);
}

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ApiService> _logger;
    private int _activeRequests = 0;
    private bool _isLoading;
    private TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);

    public event EventHandler<bool>? LoadingStateChanged;
    public event EventHandler<string>? ErrorOccurred;
    
    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                LoadingStateChanged?.Invoke(this, value);
            }
        }
    }

    public ApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ILogger<ApiService> logger)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        // Timeout is configured in Program.cs via AddHttpClient
        _defaultTimeout = _httpClient.Timeout;
    }
    
    public void SetTimeout(TimeSpan timeout)
    {
        _defaultTimeout = timeout;
        // Don't set timeout on HttpClient after it's been created
        // Instead, use CancellationTokenSource with timeout for individual requests
        _logger.LogWarning("SetTimeout called but timeout changes after HttpClient creation are not supported. Configure timeout in Program.cs instead.");
    }

    private void BeginLoading()
    {
        _activeRequests++;
        if (_activeRequests == 1)
        {
            IsLoading = true;
        }
    }

    private void EndLoading()
    {
        _activeRequests--;
        if (_activeRequests == 0)
        {
            IsLoading = false;
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
        try
        {
            BeginLoading();
            
            var fullUri = new Uri(_httpClient.BaseAddress!, endpoint);
            var uriBuilder = new UriBuilder(fullUri);
            var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
            query["_"] = DateTime.UtcNow.Ticks.ToString();
            uriBuilder.Query = query.ToString();

            var request = CreateRequestWithCookies(HttpMethod.Get, uriBuilder.Uri.PathAndQuery);
            return await ExecuteWithRetryAsync(() => _httpClient.SendAsync(request, cancellationToken), endpoint);
        }
        catch (Exception ex)
        {
            HandleError(ex, endpoint, "GET");
            throw;
        }
        finally
        {
            EndLoading();
        }
    }

    public async Task<HttpResponseMessage> PostAsJsonAsync<T>(string endpoint, T data, CancellationToken cancellationToken = default)
    {
        try
        {
            BeginLoading();
            
            var request = CreateRequestWithCookies(HttpMethod.Post, endpoint);
            var json = JsonSerializer.Serialize(data);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return await ExecuteWithRetryAsync(() => _httpClient.SendAsync(request, cancellationToken), endpoint);
        }
        catch (Exception ex)
        {
            HandleError(ex, endpoint, "POST");
            throw;
        }
        finally
        {
            EndLoading();
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
        try
        {
            BeginLoading();
            
            var request = CreateRequestWithCookies(HttpMethod.Put, endpoint);
            var json = JsonSerializer.Serialize(data);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return await ExecuteWithRetryAsync(() => _httpClient.SendAsync(request, cancellationToken), endpoint);
        }
        catch (Exception ex)
        {
            HandleError(ex, endpoint, "PUT");
            throw;
        }
        finally
        {
            EndLoading();
        }
    }

    public async Task<HttpResponseMessage> DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            BeginLoading();
            
            var request = CreateRequestWithCookies(HttpMethod.Delete, endpoint);
            return await ExecuteWithRetryAsync(() => _httpClient.SendAsync(request, cancellationToken), endpoint);
        }
        catch (Exception ex)
        {
            HandleError(ex, endpoint, "DELETE");
            throw;
        }
        finally
        {
            EndLoading();
        }
    }

    /// <summary>
    /// Executa uma requisição HTTP com retry automático em caso de falha temporária
    /// </summary>
    private async Task<HttpResponseMessage> ExecuteWithRetryAsync(
        Func<Task<HttpResponseMessage>> operation, 
        string endpoint, 
        int maxRetries = 2)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt <= maxRetries)
        {
            try
            {
                var response = await operation();
                
                // Não faz retry para erros de cliente (4xx) exceto 408 (Timeout) e 429 (Too Many Requests)
                if (!response.IsSuccessStatusCode)
                {
                    var statusCode = (int)response.StatusCode;
                    if (statusCode >= 400 && statusCode < 500 && statusCode != 408 && statusCode != 429)
                    {
                        return response; // Retorna imediatamente sem retry
                    }
                }

                // Sucesso ou erro que não deve ser retentado
                if (response.IsSuccessStatusCode || attempt == maxRetries)
                {
                    return response;
                }

                // Aguarda antes de tentar novamente (exponential backoff)
                var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 500);
                _logger.LogWarning("Tentativa {Attempt}/{MaxRetries} falhou para {Endpoint}. Status: {StatusCode}. Aguardando {Delay}ms...", 
                    attempt + 1, maxRetries + 1, endpoint, response.StatusCode, delay.TotalMilliseconds);
                await Task.Delay(delay);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                lastException = ex;
                _logger.LogWarning("Timeout na tentativa {Attempt}/{MaxRetries} para {Endpoint}", 
                    attempt + 1, maxRetries + 1, endpoint);
                
                if (attempt == maxRetries)
                    throw;
                    
                await Task.Delay(TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 500));
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Erro de rede na tentativa {Attempt}/{MaxRetries} para {Endpoint}", 
                    attempt + 1, maxRetries + 1, endpoint);
                
                if (attempt == maxRetries)
                    throw;
                    
                await Task.Delay(TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 500));
            }

            attempt++;
        }

        throw lastException ?? new Exception($"Falha ao executar requisição para {endpoint} após {maxRetries} tentativas");
    }

    /// <summary>
    /// Processa e registra erros de requisições HTTP
    /// </summary>
    private void HandleError(Exception ex, string endpoint, string method)
    {
        string errorMessage;

        if (ex is TaskCanceledException && ex.InnerException is TimeoutException)
        {
            errorMessage = $"Timeout ao executar {method} em {endpoint}";
            _logger.LogError(ex, errorMessage);
            ErrorOccurred?.Invoke(this, "A requisição demorou muito tempo. Verifique sua conexão.");
        }
        else if (ex is HttpRequestException)
        {
            errorMessage = $"Erro de rede ao executar {method} em {endpoint}";
            _logger.LogError(ex, errorMessage);
            ErrorOccurred?.Invoke(this, "Erro de conexão. Verifique sua rede e tente novamente.");
        }
        else
        {
            errorMessage = $"Erro inesperado ao executar {method} em {endpoint}";
            _logger.LogError(ex, errorMessage);
            ErrorOccurred?.Invoke(this, "Erro inesperado. Tente novamente mais tarde.");
        }
    }
}
