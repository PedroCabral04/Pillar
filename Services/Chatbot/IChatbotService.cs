using erp.DTOs.Chatbot;

namespace erp.Services.Chatbot;

public interface IChatbotService
{
    Task<ChatResponseDto> ProcessMessageAsync(
        string message,
        List<ChatMessageDto>? conversationHistory = null,
        int? userId = null,
        ChatOperationMode operationMode = ChatOperationMode.ProposeAction,
        ChatResponseStyle responseStyle = ChatResponseStyle.Executive,
        bool isConfirmedAction = false,
        int? conversationId = null,
        string source = "quick");
}
