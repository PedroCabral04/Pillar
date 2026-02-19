namespace erp.DTOs.Chatbot;

public class ChatbotAuditEntryDto
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? ConversationId { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public bool Success { get; set; }
    public bool RequiresConfirmation { get; set; }
    public ChatOperationMode OperationMode { get; set; }
    public ChatResponseStyle ResponseStyle { get; set; }
    public int DurationMs { get; set; }
    public string RequestMessagePreview { get; set; } = string.Empty;
    public string? Error { get; set; }
}
