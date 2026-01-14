using erp.Models.Chatbot;

namespace erp.DAOs.Chatbot;

public interface IChatConversationDao
{
    /// <summary>
    /// Get all conversations for a specific user.
    /// </summary>
    Task<List<ChatConversation>> GetUserConversationsAsync(int userId, bool includeArchived = false);
    
    /// <summary>
    /// Get a conversation by ID with messages.
    /// </summary>
    Task<ChatConversation?> GetConversationWithMessagesAsync(int conversationId);
    
    /// <summary>
    /// Get a conversation by ID (without messages).
    /// </summary>
    Task<ChatConversation?> GetConversationAsync(int conversationId);
    
    /// <summary>
    /// Create a new conversation.
    /// </summary>
    Task<ChatConversation> CreateConversationAsync(ChatConversation conversation);
    
    /// <summary>
    /// Add a message to a conversation.
    /// </summary>
    Task<ChatMessage> AddMessageAsync(ChatMessage message);
    
    /// <summary>
    /// Update conversation details.
    /// </summary>
    Task UpdateConversationAsync(ChatConversation conversation);
    
    /// <summary>
    /// Delete a conversation and all its messages.
    /// </summary>
    Task DeleteConversationAsync(int conversationId);
    
    /// <summary>
    /// Get message count for a conversation.
    /// </summary>
    Task<int> GetMessageCountAsync(int conversationId);
    
    /// <summary>
    /// Get the next message order for a conversation.
    /// </summary>
    Task<int> GetNextMessageOrderAsync(int conversationId);
}
