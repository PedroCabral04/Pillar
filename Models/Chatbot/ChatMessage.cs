using erp.Models.Audit;

namespace erp.Models.Chatbot;

/// <summary>
/// Represents a single message in a chatbot conversation.
/// </summary>
public class ChatMessage : IAuditable
{
    public int Id { get; set; }
    
    /// <summary>
    /// The conversation this message belongs to.
    /// </summary>
    public int ConversationId { get; set; }
    
    /// <summary>
    /// Message role: "user" or "assistant".
    /// </summary>
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// Message content (Markdown supported).
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// When the message was sent.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Whether this message represents an error response.
    /// </summary>
    public bool IsError { get; set; }
    
    /// <summary>
    /// Order of the message in the conversation.
    /// </summary>
    public int Order { get; set; }
    
    // Navigation property
    public virtual ChatConversation? Conversation { get; set; }
}
