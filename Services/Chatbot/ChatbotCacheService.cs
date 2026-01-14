using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using erp.DTOs.Chatbot;

namespace erp.Services.Chatbot;

/// <summary>
/// Implementação do serviço de cache para o chatbot
/// </summary>
public class ChatbotCacheService : IChatbotCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<ChatbotCacheService> _logger;
    private readonly IConfiguration _configuration;

    // Configurações de cache
    private readonly bool _enabled;
    private readonly TimeSpan _responseTtl;
    private readonly TimeSpan _pluginDataTtl;
    private readonly bool _cacheOnlyExactMatches;

    // Prefixos para chaves de cache
    private const string ResponsePrefix = "chatbot:response:";
    private const string PluginPrefix = "chatbot:plugin:";

    // Estatísticas (thread-safe)
    private int _responseCacheHits;
    private int _responseCacheMisses;
    private int _pluginCacheHits;
    private int _pluginCacheMisses;

    // Conjunto de chaves de plugins para invalidação seletiva
    private readonly ConcurrentDictionary<string, HashSet<string>> _pluginCacheKeys = new();

    public ChatbotCacheService(
        IMemoryCache memoryCache,
        ILogger<ChatbotCacheService> logger,
        IConfiguration configuration)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _configuration = configuration;

        // Carregar configurações
        _enabled = configuration.GetValue("Chatbot:Cache:Enabled", true);
        _responseTtl = TimeSpan.FromSeconds(configuration.GetValue("Chatbot:Cache:ResponseTTLSeconds", 300)); // 5 min default
        _pluginDataTtl = TimeSpan.FromSeconds(configuration.GetValue("Chatbot:Cache:PluginDataTTLSeconds", 60)); // 1 min default
        _cacheOnlyExactMatches = configuration.GetValue("Chatbot:Cache:OnlyExactMatches", false);

        if (_enabled)
        {
            _logger.LogInformation(
                "Cache do chatbot habilitado. TTL respostas: {ResponseTtl}s, TTL plugins: {PluginTtl}s",
                _responseTtl.TotalSeconds,
                _pluginDataTtl.TotalSeconds);
        }
        else
        {
            _logger.LogInformation("Cache do chatbot desabilitado");
        }
    }

    public bool IsEnabled => _enabled;

    public ChatResponseDto? GetCachedResponse(string message, string? contextHash = null)
    {
        if (!_enabled) return null;

        var key = GenerateResponseCacheKey(message, contextHash);
        
        if (_memoryCache.TryGetValue(key, out ChatResponseDto? cachedResponse))
        {
            Interlocked.Increment(ref _responseCacheHits);
            _logger.LogDebug("Cache hit para mensagem: {MessagePreview}...", 
                message.Length > 30 ? message[..30] : message);
            return cachedResponse;
        }

        Interlocked.Increment(ref _responseCacheMisses);
        return null;
    }

    public void SetCachedResponse(string message, ChatResponseDto response, string? contextHash = null)
    {
        if (!_enabled) return;
        if (!response.Success) return; // Não cachear respostas com erro

        var key = GenerateResponseCacheKey(message, contextHash);
        
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(_responseTtl)
            .SetSize(1); // Para controle de memória se SizeLimit for configurado

        _memoryCache.Set(key, response, cacheOptions);
        
        _logger.LogDebug("Resposta cacheada para mensagem: {MessagePreview}...", 
            message.Length > 30 ? message[..30] : message);
    }

    public T? GetPluginData<T>(string pluginName, string functionName, string? parameters = null)
    {
        if (!_enabled) return default;

        var key = GeneratePluginCacheKey(pluginName, functionName, parameters);
        
        if (_memoryCache.TryGetValue(key, out T? data))
        {
            Interlocked.Increment(ref _pluginCacheHits);
            _logger.LogDebug("Cache hit para plugin {Plugin}.{Function}", pluginName, functionName);
            return data;
        }

        Interlocked.Increment(ref _pluginCacheMisses);
        return default;
    }

    public void SetPluginData<T>(string pluginName, string functionName, T data, string? parameters = null)
    {
        if (!_enabled) return;
        if (data == null) return;

        var key = GeneratePluginCacheKey(pluginName, functionName, parameters);
        
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(_pluginDataTtl)
            .SetSize(1);

        _memoryCache.Set(key, data, cacheOptions);
        
        // Registrar a chave para invalidação futura
        var pluginKeys = _pluginCacheKeys.GetOrAdd(pluginName, _ => new HashSet<string>());
        lock (pluginKeys)
        {
            pluginKeys.Add(key);
        }

        _logger.LogDebug("Dados cacheados para plugin {Plugin}.{Function}", pluginName, functionName);
    }

    public void InvalidatePluginCache(string pluginName)
    {
        if (_pluginCacheKeys.TryGetValue(pluginName, out var keys))
        {
            lock (keys)
            {
                foreach (var key in keys)
                {
                    _memoryCache.Remove(key);
                }
                keys.Clear();
            }
            _logger.LogInformation("Cache do plugin {Plugin} invalidado", pluginName);
        }
    }

    public void InvalidateResponseCache()
    {
        // IMemoryCache não tem método para listar chaves, então não podemos invalidar seletivamente
        // Esta é uma limitação conhecida. Para invalidação granular, considerar IDistributedCache com Redis
        _logger.LogWarning("Invalidação de cache de respostas solicitada. " +
            "Nota: IMemoryCache não suporta invalidação em massa. As entradas expirarão naturalmente.");
    }

    public string GenerateContextHash(List<ChatMessageDto>? conversationHistory, int messageCount = 2)
    {
        if (conversationHistory == null || conversationHistory.Count == 0)
            return "no-context";

        // Pegar apenas as últimas N mensagens para o hash
        var relevantMessages = conversationHistory
            .TakeLast(messageCount)
            .Select(m => $"{m.Role}:{NormalizeMessage(m.Content)}")
            .ToList();

        var combinedContent = string.Join("|", relevantMessages);
        return ComputeHash(combinedContent);
    }

    public ChatCacheStatistics GetStatistics()
    {
        return new ChatCacheStatistics
        {
            ResponseCacheHits = _responseCacheHits,
            ResponseCacheMisses = _responseCacheMisses,
            PluginCacheHits = _pluginCacheHits,
            PluginCacheMisses = _pluginCacheMisses
        };
    }

    #region Private Methods

    private string GenerateResponseCacheKey(string message, string? contextHash)
    {
        var normalizedMessage = NormalizeMessage(message);
        var messageHash = ComputeHash(normalizedMessage);
        
        if (string.IsNullOrEmpty(contextHash) || _cacheOnlyExactMatches)
        {
            return $"{ResponsePrefix}{messageHash}";
        }
        
        return $"{ResponsePrefix}{messageHash}:{contextHash}";
    }

    private string GeneratePluginCacheKey(string pluginName, string functionName, string? parameters)
    {
        var baseKey = $"{PluginPrefix}{pluginName}:{functionName}";
        
        if (string.IsNullOrEmpty(parameters))
        {
            return baseKey;
        }
        
        var paramHash = ComputeHash(parameters);
        return $"{baseKey}:{paramHash}";
    }

    private string NormalizeMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return string.Empty;

        // Normalização básica: lowercase, trim, remover múltiplos espaços
        return string.Join(" ", message
            .ToLowerInvariant()
            .Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        // Usar apenas os primeiros 16 bytes para uma chave mais curta
        return Convert.ToHexString(bytes, 0, 8).ToLowerInvariant();
    }

    #endregion
}
