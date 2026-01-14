using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace erp.Security;

/// <summary>
/// Middleware that validates API keys for requests to /api endpoints.
/// Uses timing-safe comparison to prevent timing attacks.
/// </summary>
public class ApiKeyMiddleware
{
    private const string HeaderName = "X-Api-Key";
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyMiddleware> _logger;
    private readonly SecurityOptions _options;
    private readonly IMemoryCache _rateLimitCache;
    
    // Rate limit: 100 requests per minute per IP (more reasonable than 1/sec)
    private const int MaxRequestsPerWindow = 100;
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(1);
    
    // Precomputed hash of the API key for timing-safe comparison without length oracle
    private readonly byte[]? _expectedKeyHash;

    public ApiKeyMiddleware(
        RequestDelegate next,
        IOptions<SecurityOptions> options,
        ILogger<ApiKeyMiddleware> logger,
        IMemoryCache memoryCache)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
        _rateLimitCache = memoryCache;
        
        // Precompute hash of expected API key to avoid length oracle
        if (!string.IsNullOrEmpty(_options.ApiKey))
        {
            _expectedKeyHash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(_options.ApiKey));
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip API key validation if not required
        if (!_options.RequireApiKey || string.IsNullOrEmpty(_options.ApiKey))
        {
            await _next(context);
            return;
        }

        // Only enforce for API routes
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        // Check rate limiting using sliding window with MemoryCache
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var cacheKey = $"ratelimit:{remoteIp}";
        
        var requestCount = _rateLimitCache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = RateLimitWindow;
            return 0;
        });

        if (requestCount >= MaxRequestsPerWindow)
        {
            _logger.LogWarning("Rate limit exceeded for IP: {RemoteIp} ({Count} requests in window)", 
                remoteIp, requestCount);
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers["Retry-After"] = "60";
            await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
            return;
        }

        // Increment request count
        _rateLimitCache.Set(cacheKey, requestCount + 1, RateLimitWindow);

        // Validate API key
        if (!context.Request.Headers.TryGetValue(HeaderName, out var provided) ||
            provided.Count == 0)
        {
            _logger.LogWarning("API request without API key from IP: {RemoteIp}", remoteIp);
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsync("Missing API key");
            return;
        }

        // Use timing-safe comparison with hash to prevent timing attacks (including length oracle)
        var providedKey = provided.ToString()!;
        if (!TimingSafeEquals(providedKey))
        {
            _logger.LogWarning("Invalid API key from IP: {RemoteIp}", remoteIp);
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsync("Invalid API key");
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// Compares the provided key against the expected key in constant time.
    /// Uses hash comparison to eliminate length oracle vulnerability.
    /// </summary>
    private bool TimingSafeEquals(string providedKey)
    {
        if (_expectedKeyHash == null)
            return false;

        // Hash the provided key - this makes comparison constant time regardless of input length
        var providedHash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(providedKey));
        
        // Compare hashes in constant time
        return CryptographicOperations.FixedTimeEquals(_expectedKeyHash, providedHash);
    }
}
