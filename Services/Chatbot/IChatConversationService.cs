using erp.DTOs.Chatbot;

namespace erp.Services.Chatbot;

public interface IChatConversationService
{
    /// <summary>
    /// Get all conversations for the current user.
    /// </summary>
    Task<List<ChatConversationListDto>> GetUserConversationsAsync(int userId, bool includeArchived = false);
    
    /// <summary>
    /// Get a conversation with all messages (only if owned by user).
    /// </summary>
    Task<ChatConversationDto?> GetConversationAsync(int userId, int conversationId);
    
    /// <summary>
    /// Create a new conversation, optionally with an initial message.
    /// </summary>
    Task<ChatConversationDto> CreateConversationAsync(int userId, CreateConversationDto request);
    
    /// <summary>
    /// Send a message in a conversation and get AI response.
    /// Enforces the 20 message limit.
    /// </summary>
    Task<ConversationMessageResponseDto> SendMessageAsync(int userId, int conversationId, string message);
    
    /// <summary>
    /// Update conversation title or archive status.
    /// </summary>
    Task<bool> UpdateConversationAsync(int userId, int conversationId, UpdateConversationDto request);
    
    /// <summary>
    /// Delete a conversation (only if owned by user).
    /// </summary>
    Task<bool> DeleteConversationAsync(int userId, int conversationId);
}
