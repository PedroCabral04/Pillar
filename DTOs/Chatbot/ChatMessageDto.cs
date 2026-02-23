namespace erp.DTOs.Chatbot;

public enum ChatOperationMode
{
    ReadOnly = 0,
    ProposeAction = 1,
    ExecuteWithConfirmation = 2
}

public enum ChatResponseStyle
{
    Executive = 0,
    Specialist = 1
}

public class ChatEvidenceSourceDto
{
    public string Source { get; set; } = string.Empty;
    public string? Scope { get; set; }
    public string? Period { get; set; }
}

public class ChatMessageDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Role { get; set; } = string.Empty; // "user" ou "assistant"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsError { get; set; }
}

public class ChatRequestDto
{
    public string Message { get; set; } = string.Empty;
    public List<ChatMessageDto>? ConversationHistory { get; set; }
    public ChatOperationMode OperationMode { get; set; } = ChatOperationMode.ProposeAction;
    public ChatResponseStyle ResponseStyle { get; set; } = ChatResponseStyle.Executive;
}

public class ChatResponseDto
{
    public string Response { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<string>? SuggestedActions { get; set; }
    public ChatOperationMode OperationMode { get; set; } = ChatOperationMode.ProposeAction;
    public bool RequiresConfirmation { get; set; }
    public string? ConfirmationPrompt { get; set; }
    public List<ChatEvidenceSourceDto>? EvidenceSources { get; set; }
}
