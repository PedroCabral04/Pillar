using erp.Data;
using erp.Models.Chatbot;
using Microsoft.EntityFrameworkCore;

namespace erp.DAOs.Chatbot;

public class ChatConversationDao : IChatConversationDao
{
    private readonly ApplicationDbContext _context;

    public ChatConversationDao(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ChatConversation>> GetUserConversationsAsync(int userId, bool includeArchived = false)
    {
        var query = _context.ChatConversations
            .AsNoTracking()
            .Where(c => c.UserId == userId);

        if (!includeArchived)
        {
            query = query.Where(c => !c.IsArchived);
        }

        return await query
            .Include(c => c.Messages)
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .ToListAsync();
    }

    public async Task<ChatConversation?> GetConversationWithMessagesAsync(int conversationId)
    {
        return await _context.ChatConversations
            .AsNoTracking()
            .Include(c => c.Messages.OrderBy(m => m.Order))
            .FirstOrDefaultAsync(c => c.Id == conversationId);
    }

    public async Task<ChatConversation?> GetConversationAsync(int conversationId)
    {
        return await _context.ChatConversations
            .FirstOrDefaultAsync(c => c.Id == conversationId);
    }

    public async Task<ChatConversation> CreateConversationAsync(ChatConversation conversation)
    {
        _context.ChatConversations.Add(conversation);
        await _context.SaveChangesAsync();
        return conversation;
    }

    public async Task<ChatMessage> AddMessageAsync(ChatMessage message)
    {
        _context.ChatMessages.Add(message);
        
        // Update conversation's LastMessageAt
        var conversation = await _context.ChatConversations.FindAsync(message.ConversationId);
        if (conversation != null)
        {
            conversation.LastMessageAt = message.Timestamp;
        }
        
        await _context.SaveChangesAsync();
        return message;
    }

    public async Task UpdateConversationAsync(ChatConversation conversation)
    {
        _context.ChatConversations.Update(conversation);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteConversationAsync(int conversationId)
    {
        var conversation = await _context.ChatConversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == conversationId);
        
        if (conversation != null)
        {
            _context.ChatMessages.RemoveRange(conversation.Messages);
            _context.ChatConversations.Remove(conversation);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetMessageCountAsync(int conversationId)
    {
        return await _context.ChatMessages
            .CountAsync(m => m.ConversationId == conversationId);
    }

    public async Task<int> GetNextMessageOrderAsync(int conversationId)
    {
        var maxOrder = await _context.ChatMessages
            .Where(m => m.ConversationId == conversationId)
            .MaxAsync(m => (int?)m.Order) ?? -1;
        
        return maxOrder + 1;
    }
}
