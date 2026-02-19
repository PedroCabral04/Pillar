using Microsoft.Extensions.Caching.Memory;

namespace erp.Services.Chatbot;

public class ChatbotConfirmationService : IChatbotConfirmationService
{
    private static readonly TimeSpan PendingConfirmationTtl = TimeSpan.FromMinutes(10);
    private readonly IMemoryCache _memoryCache;

    public ChatbotConfirmationService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public void SetPendingAction(int userId, int? conversationId, string source, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        _memoryCache.Set(BuildKey(userId, conversationId, source), message, PendingConfirmationTtl);
    }

    public string? GetPendingAction(int userId, int? conversationId, string source)
    {
        _memoryCache.TryGetValue(BuildKey(userId, conversationId, source), out string? pendingAction);
        return pendingAction;
    }

    public void ClearPendingAction(int userId, int? conversationId, string source)
    {
        _memoryCache.Remove(BuildKey(userId, conversationId, source));
    }

    private static string BuildKey(int userId, int? conversationId, string source)
    {
        var normalizedSource = string.IsNullOrWhiteSpace(source) ? "quick" : source.Trim().ToLowerInvariant();
        var conversationKey = conversationId?.ToString() ?? "none";
        return $"chatbot:pending-confirmation:{userId}:{normalizedSource}:{conversationKey}";
    }
}
