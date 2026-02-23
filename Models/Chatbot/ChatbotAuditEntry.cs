namespace erp.Models.Chatbot;

/// <summary>
/// Dedicated chatbot audit trail for message processing, confirmations and guardrails.
/// </summary>
public class ChatbotAuditEntry
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? UserId { get; set; }
    public int? TenantId { get; set; }
    public int? ConversationId { get; set; }

    public string Source { get; set; } = "quick";
    public string Outcome { get; set; } = string.Empty;

    public string RequestMessage { get; set; } = string.Empty;
    public string EffectiveMessage { get; set; } = string.Empty;
    public string? ResponseMessage { get; set; }
    public string? Error { get; set; }

    public int OperationMode { get; set; }
    public int ResponseStyle { get; set; }
    public bool IsConfirmedAction { get; set; }
    public bool RequiresConfirmation { get; set; }
    public bool Success { get; set; }

    public string? SuggestedActionsJson { get; set; }
    public string? EvidenceSourcesJson { get; set; }

    public string? AiProvider { get; set; }
    public bool AiConfigured { get; set; }
    public int DurationMs { get; set; }
}
