using erp.Models.Audit;
using erp.Models.Identity;

namespace erp.Models.Chatbot;

/// <summary>
/// Represents a chatbot conversation belonging to a specific user.
/// Each conversation stores up to 20 messages.
/// </summary>
public class ChatConversation : IAuditable, IMustHaveTenant
{
    public int Id { get; set; }
    
    /// <summary>
    /// The user who owns this conversation. Only this user can view it.
    /// </summary>
    public int UserId { get; set; }
    
    /// <summary>
    /// Tenant isolation.
    /// </summary>
    public int TenantId { get; set; }
    
    /// <summary>
    /// Auto-generated title from first user message (truncated).
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// When the conversation was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last activity timestamp.
    /// </summary>
    public DateTime? LastMessageAt { get; set; }
    
    /// <summary>
    /// Soft archive flag.
    /// </summary>
    public bool IsArchived { get; set; }
    
    /// <summary>
    /// Maximum number of messages allowed in this conversation.
    /// </summary>
    public const int MaxMessages = 20;
    
    // Navigation properties
    public virtual ApplicationUser? User { get; set; }
    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
