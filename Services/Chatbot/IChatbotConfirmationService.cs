namespace erp.Services.Chatbot;

public interface IChatbotConfirmationService
{
    void SetPendingAction(int userId, int? conversationId, string source, string message);
    string? GetPendingAction(int userId, int? conversationId, string source);
    void ClearPendingAction(int userId, int? conversationId, string source);
}
