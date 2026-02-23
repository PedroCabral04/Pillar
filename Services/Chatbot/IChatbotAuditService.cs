using erp.DTOs.Chatbot;

namespace erp.Services.Chatbot;

public class ChatbotAuditRequest
{
    public int? UserId { get; set; }
    public int? ConversationId { get; set; }
    public string Source { get; set; } = "quick";
    public string Outcome { get; set; } = string.Empty;
    public string RequestMessage { get; set; } = string.Empty;
    public string EffectiveMessage { get; set; } = string.Empty;
    public ChatResponseDto? Response { get; set; }
    public ChatOperationMode OperationMode { get; set; }
    public ChatResponseStyle ResponseStyle { get; set; }
    public bool IsConfirmedAction { get; set; }
    public string? AiProvider { get; set; }
    public bool AiConfigured { get; set; }
    public int DurationMs { get; set; }
}

public interface IChatbotAuditService
{
    Task LogAsync(ChatbotAuditRequest request);
    Task<List<ChatbotAuditEntryDto>> GetRecentByUserAsync(int userId, int take = 30);
}
