namespace erp.DTOs.Chatbot;

/// <summary>
/// DTO for listing conversations in sidebar.
/// </summary>
public class ChatConversationListDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int MessageCount { get; set; }
    public bool IsArchived { get; set; }
}

/// <summary>
/// Full conversation with messages.
/// </summary>
public class ChatConversationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public bool IsArchived { get; set; }
    public List<ChatMessageDto> Messages { get; set; } = new();
    public bool IsAtMessageLimit { get; set; }
}

/// <summary>
/// Request to create a new conversation.
/// </summary>
public class CreateConversationDto
{
    /// <summary>
    /// Optional: First message to send. If provided, conversation title is auto-generated.
    /// </summary>
    public string? InitialMessage { get; set; }
}

/// <summary>
/// Request to send a message in a conversation.
/// </summary>
public class SendMessageToConversationDto
{
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response from sending a message in a conversation.
/// </summary>
public class ConversationMessageResponseDto
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public ChatMessageDto? UserMessage { get; set; }
    public ChatMessageDto? AssistantMessage { get; set; }
    public List<string>? SuggestedActions { get; set; }
    public bool IsAtMessageLimit { get; set; }
}

/// <summary>
/// Request to update conversation details.
/// </summary>
public class UpdateConversationDto
{
    public string? Title { get; set; }
    public bool? IsArchived { get; set; }
}
