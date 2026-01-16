namespace erp.Services.Chatbot;

/// <summary>
/// Interface for accessing the current user context within chatbot plugins.
/// This provides thread-safe access to the current user ID for the duration of a chatbot request.
/// </summary>
public interface IChatbotUserContext
{
    /// <summary>
    /// Gets the current user ID for this chatbot request.
    /// Returns null if no user is set (e.g., during design-time operations).
    /// </summary>
    int? CurrentUserId { get; }

    /// <summary>
    /// Sets the current user ID for the scope of this chatbot request.
    /// Should be called by the controller before processing a message.
    /// </summary>
    void SetCurrentUser(int userId);

    /// <summary>
    /// Clears the current user context.
    /// Should be called after processing is complete.
    /// </summary>
    void Clear();
}
