using erp.Models.Chatbot;

namespace erp.DAOs.Chatbot;

public interface IChatbotAuditDao
{
    Task AddAsync(ChatbotAuditEntry entry);
    Task<List<ChatbotAuditEntry>> GetRecentByUserAsync(int userId, int take = 30);
}
