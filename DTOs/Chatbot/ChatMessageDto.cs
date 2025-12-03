namespace erp.DTOs.Chatbot;

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
}

public class ChatResponseDto
{
    public string Response { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<string>? SuggestedActions { get; set; }
}
