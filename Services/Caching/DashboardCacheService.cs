using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;

namespace erp.Services.Caching;

/// <summary>
/// Service for caching dashboard data to reduce database load
/// </summary>
public interface IDashboardCacheService
{
    Task<T> GetOrSetAsync<T>(
        string cacheKey,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken ct = default);

    void Invalidate(string cacheKey);
    void InvalidateAll();
}

/// <summary>
/// Implementation of dashboard cache service using IMemoryCache
/// </summary>
public class DashboardCacheService : IDashboardCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<DashboardCacheService> _logger;

    // Default cache expiration times
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ShortExpiration = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan LongExpiration = TimeSpan.FromMinutes(15);

    public DashboardCacheService(IMemoryCache cache, ILogger<DashboardCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<T> GetOrSetAsync<T>(
        string cacheKey,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken ct = default)
    {
        var exp = expiration ?? DefaultExpiration;

        if (_cache.TryGetValue<T>(cacheKey, out var cached))
        {
            _logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
            return Task.FromResult(cached);
        }

        _logger.LogDebug("Cache miss for key: {CacheKey}", cacheKey);

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(exp)
            .SetSize(1);

        var task = factory();

        // ContinueWith to cache the result after completion
        task.ContinueWith(t =>
        {
            if (!t.IsFaulted && !t.IsCanceled)
            {
                _cache.Set(cacheKey, t.Result, cacheOptions);
                _logger.LogDebug("Cached result for key: {CacheKey}", cacheKey);
            }
        }, ct);

        return task;
    }

    public void Invalidate(string cacheKey)
    {
        _cache.Remove(cacheKey);
        _logger.LogDebug("Invalidated cache for key: {CacheKey}", cacheKey);
    }

    public void InvalidateAll()
    {
        // IMemoryCache doesn't support clearing all entries
        // This is a known limitation. For production, consider using IDistributedCache with Redis
        _logger.LogWarning("InvalidateAll called - note that IMemoryCache doesn't support clearing all entries");
    }

    /// <summary>
    /// Generates a cache key for dashboard widgets
    /// </summary>
    public static string GenerateCacheKey(string widgetType, int? tenantId, object? parameters = null)
    {
        var parts = new List<string> { "dashboard", widgetType };

        if (tenantId.HasValue)
            parts.Add($"tenant{tenantId.Value}");

        if (parameters != null)
        {
            // Create a hash from parameters to keep cache key manageable
            var paramHash = ComputeHash(parameters.ToString() ?? "");
            parts.Add(paramHash);
        }

        return string.Join(":", parts);
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes, 0, 8).ToLowerInvariant();
    }
}
