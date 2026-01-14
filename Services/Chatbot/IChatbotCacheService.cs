using erp.DTOs.Chatbot;

namespace erp.Services.Chatbot;

/// <summary>
/// Serviço de cache para o chatbot para reduzir chamadas à API de IA
/// </summary>
public interface IChatbotCacheService
{
    /// <summary>
    /// Tenta obter uma resposta cacheada para a mensagem
    /// </summary>
    /// <param name="message">Mensagem do usuário</param>
    /// <param name="contextHash">Hash do contexto da conversa (últimas mensagens)</param>
    /// <returns>Resposta cacheada ou null se não encontrada</returns>
    ChatResponseDto? GetCachedResponse(string message, string? contextHash = null);

    /// <summary>
    /// Armazena uma resposta no cache
    /// </summary>
    /// <param name="message">Mensagem do usuário</param>
    /// <param name="response">Resposta a ser cacheada</param>
    /// <param name="contextHash">Hash do contexto da conversa</param>
    void SetCachedResponse(string message, ChatResponseDto response, string? contextHash = null);

    /// <summary>
    /// Obtém dados cacheados de um plugin
    /// </summary>
    /// <typeparam name="T">Tipo do dado</typeparam>
    /// <param name="pluginName">Nome do plugin</param>
    /// <param name="functionName">Nome da função</param>
    /// <param name="parameters">Parâmetros serializados</param>
    /// <returns>Dados cacheados ou default se não encontrado</returns>
    T? GetPluginData<T>(string pluginName, string functionName, string? parameters = null);

    /// <summary>
    /// Armazena dados de um plugin no cache
    /// </summary>
    /// <typeparam name="T">Tipo do dado</typeparam>
    /// <param name="pluginName">Nome do plugin</param>
    /// <param name="functionName">Nome da função</param>
    /// <param name="data">Dados a serem cacheados</param>
    /// <param name="parameters">Parâmetros serializados</param>
    void SetPluginData<T>(string pluginName, string functionName, T data, string? parameters = null);

    /// <summary>
    /// Invalida todo o cache de um plugin específico
    /// </summary>
    /// <param name="pluginName">Nome do plugin</param>
    void InvalidatePluginCache(string pluginName);

    /// <summary>
    /// Invalida todo o cache de respostas
    /// </summary>
    void InvalidateResponseCache();

    /// <summary>
    /// Gera um hash para o contexto da conversa
    /// </summary>
    /// <param name="conversationHistory">Histórico da conversa</param>
    /// <param name="messageCount">Número de mensagens a considerar para o hash</param>
    /// <returns>Hash do contexto</returns>
    string GenerateContextHash(List<ChatMessageDto>? conversationHistory, int messageCount = 2);

    /// <summary>
    /// Obtém estatísticas do cache
    /// </summary>
    ChatCacheStatistics GetStatistics();

    /// <summary>
    /// Verifica se o cache está habilitado
    /// </summary>
    bool IsEnabled { get; }
}

/// <summary>
/// Estatísticas do cache do chatbot
/// </summary>
public class ChatCacheStatistics
{
    public int ResponseCacheHits { get; set; }
    public int ResponseCacheMisses { get; set; }
    public int PluginCacheHits { get; set; }
    public int PluginCacheMisses { get; set; }
    public double ResponseHitRate => ResponseCacheHits + ResponseCacheMisses > 0
        ? (double)ResponseCacheHits / (ResponseCacheHits + ResponseCacheMisses) * 100
        : 0;
    public double PluginHitRate => PluginCacheHits + PluginCacheMisses > 0
        ? (double)PluginCacheHits / (PluginCacheHits + PluginCacheMisses) * 100
        : 0;
    public int EstimatedApiCallsSaved => ResponseCacheHits;
}
