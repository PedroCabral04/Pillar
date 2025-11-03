using erp.DTOs.Chatbot;

namespace erp.Services.Chatbot;

public interface IChatbotService
{
    Task<ChatResponseDto> ProcessMessageAsync(string message, List<ChatMessageDto>? conversationHistory = null);
}
