namespace erp.Services.Chatbot;

/// <summary>
/// Async local storage for chatbot user context.
/// Uses AsyncLocal to ensure thread-safe access to user context across async operations.
/// </summary>
public class ChatbotUserContext : IChatbotUserContext
{
    private static readonly System.Threading.AsyncLocal<int?> _currentUserId = new();

    public int? CurrentUserId => _currentUserId.Value;

    public void SetCurrentUser(int userId)
    {
        _currentUserId.Value = userId;
    }

    public void Clear()
    {
        _currentUserId.Value = null;
    }
}
