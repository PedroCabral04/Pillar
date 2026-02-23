using erp.Data;
using erp.Models.Chatbot;
using Microsoft.EntityFrameworkCore;

namespace erp.DAOs.Chatbot;

public class ChatbotAuditDao : IChatbotAuditDao
{
    private readonly ApplicationDbContext _context;

    public ChatbotAuditDao(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ChatbotAuditEntry entry)
    {
        _context.Set<ChatbotAuditEntry>().Add(entry);
        await _context.SaveChangesAsync();
    }

    public async Task<List<ChatbotAuditEntry>> GetRecentByUserAsync(int userId, int take = 30)
    {
        return await _context.Set<ChatbotAuditEntry>()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .ToListAsync();
    }
}
