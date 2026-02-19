using erp.DAOs.Chatbot;
using erp.DTOs.Chatbot;
using erp.Models.Chatbot;

namespace erp.Services.Chatbot;

public class ChatConversationService : IChatConversationService
{
    private readonly IChatConversationDao _conversationDao;
    private readonly IChatbotService _chatbotService;
    private readonly ILogger<ChatConversationService> _logger;
    
    private const int MaxTitleLength = 100;

    public ChatConversationService(
        IChatConversationDao conversationDao,
        IChatbotService chatbotService,
        ILogger<ChatConversationService> logger)
    {
        _conversationDao = conversationDao;
        _chatbotService = chatbotService;
        _logger = logger;
    }

    public async Task<List<ChatConversationListDto>> GetUserConversationsAsync(int userId, bool includeArchived = false)
    {
        var conversations = await _conversationDao.GetUserConversationsAsync(userId, includeArchived);
        
        return conversations.Select(c => new ChatConversationListDto
        {
            Id = c.Id,
            Title = c.Title,
            CreatedAt = c.CreatedAt,
            LastMessageAt = c.LastMessageAt,
            MessageCount = c.Messages.Count,
            IsArchived = c.IsArchived
        }).ToList();
    }

    public async Task<ChatConversationDto?> GetConversationAsync(int userId, int conversationId)
    {
        var conversation = await _conversationDao.GetConversationWithMessagesAsync(conversationId);
        
        // Verify ownership
        if (conversation == null || conversation.UserId != userId)
        {
            return null;
        }
        
        return new ChatConversationDto
        {
            Id = conversation.Id,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            LastMessageAt = conversation.LastMessageAt,
            IsArchived = conversation.IsArchived,
            OperationMode = NormalizeOperationMode(conversation.DefaultOperationMode),
            ResponseStyle = NormalizeResponseStyle(conversation.DefaultResponseStyle),
            IsAtMessageLimit = conversation.Messages.Count >= ChatConversation.MaxMessages,
            Messages = conversation.Messages.Select(m => new ChatMessageDto
            {
                Id = m.Id.ToString(),
                Role = m.Role,
                Content = m.Content,
                Timestamp = m.Timestamp,
                IsError = m.IsError
            }).ToList()
        };
    }

    public async Task<ChatConversationDto> CreateConversationAsync(int userId, CreateConversationDto request)
    {
        var conversation = new ChatConversation
        {
            UserId = userId,
            Title = "Nova conversa",
            CreatedAt = DateTime.UtcNow,
            DefaultOperationMode = (int)request.OperationMode,
            DefaultResponseStyle = (int)request.ResponseStyle
        };
        
        conversation = await _conversationDao.CreateConversationAsync(conversation);
        
        // If there's an initial message, send it
        if (!string.IsNullOrWhiteSpace(request.InitialMessage))
        {
            await SendMessageAsync(
                userId,
                conversation.Id,
                request.InitialMessage,
                request.OperationMode,
                request.ResponseStyle);
            
            // Reload to get messages
            var updated = await _conversationDao.GetConversationWithMessagesAsync(conversation.Id);
            if (updated != null)
            {
                return new ChatConversationDto
                {
                    Id = updated.Id,
                    Title = updated.Title,
                    CreatedAt = updated.CreatedAt,
                    LastMessageAt = updated.LastMessageAt,
                    IsArchived = updated.IsArchived,
                    OperationMode = NormalizeOperationMode(updated.DefaultOperationMode),
                    ResponseStyle = NormalizeResponseStyle(updated.DefaultResponseStyle),
                    IsAtMessageLimit = updated.Messages.Count >= ChatConversation.MaxMessages,
                    Messages = updated.Messages.Select(m => new ChatMessageDto
                    {
                        Id = m.Id.ToString(),
                        Role = m.Role,
                        Content = m.Content,
                        Timestamp = m.Timestamp,
                        IsError = m.IsError
                    }).ToList()
                };
            }
        }
        
        return new ChatConversationDto
        {
            Id = conversation.Id,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            LastMessageAt = conversation.LastMessageAt,
            IsArchived = conversation.IsArchived,
            OperationMode = NormalizeOperationMode(conversation.DefaultOperationMode),
            ResponseStyle = NormalizeResponseStyle(conversation.DefaultResponseStyle),
            IsAtMessageLimit = false,
            Messages = new List<ChatMessageDto>()
        };
    }

    public async Task<ConversationMessageResponseDto> SendMessageAsync(
        int userId,
        int conversationId,
        string message,
        ChatOperationMode operationMode = ChatOperationMode.ProposeAction,
        ChatResponseStyle responseStyle = ChatResponseStyle.Executive)
    {
        var conversation = await _conversationDao.GetConversationWithMessagesAsync(conversationId);
        
        // Verify ownership
        if (conversation == null || conversation.UserId != userId)
        {
            return new ConversationMessageResponseDto
            {
                Success = false,
                Error = "Conversa não encontrada",
                OperationMode = operationMode
            };
        }
        
        // Check message limit
        var currentCount = conversation.Messages.Count;
        if (currentCount >= ChatConversation.MaxMessages)
        {
            return new ConversationMessageResponseDto
            {
                Success = false,
                Error = "Esta conversa atingiu o limite de 20 mensagens. Inicie uma nova conversa para continuar.",
                IsAtMessageLimit = true,
                OperationMode = operationMode
            };
        }
        
        var now = DateTime.UtcNow;
        var nextOrder = await _conversationDao.GetNextMessageOrderAsync(conversationId);

        var normalizedOperationMode = NormalizeOperationMode((int)operationMode);
        var normalizedResponseStyle = NormalizeResponseStyle((int)responseStyle);

        if (conversation.DefaultOperationMode != (int)normalizedOperationMode
            || conversation.DefaultResponseStyle != (int)normalizedResponseStyle)
        {
            conversation.DefaultOperationMode = (int)normalizedOperationMode;
            conversation.DefaultResponseStyle = (int)normalizedResponseStyle;
            await _conversationDao.UpdateConversationAsync(conversation);
        }
        
        // Create user message
        var userMessage = new ChatMessage
        {
            ConversationId = conversationId,
            Role = "user",
            Content = message,
            Timestamp = now,
            Order = nextOrder
        };
        
        // Auto-generate title from first user message (before adding, so count check works)
        var shouldGenerateTitle = conversation.Title == "Nova conversa" && conversation.Messages.Count == 0;
        
        userMessage = await _conversationDao.AddMessageAsync(userMessage);
        currentCount++;
        
        // Update title if this was the first message
        if (shouldGenerateTitle)
        {
            conversation.Title = GenerateTitle(message);
            await _conversationDao.UpdateConversationAsync(conversation);
        }
        
        // Check if we can add assistant response
        if (currentCount >= ChatConversation.MaxMessages)
        {
            return new ConversationMessageResponseDto
            {
                Success = true,
                UserMessage = new ChatMessageDto
                {
                    Id = userMessage.Id.ToString(),
                    Role = userMessage.Role,
                    Content = userMessage.Content,
                    Timestamp = userMessage.Timestamp,
                    IsError = userMessage.IsError
                },
                IsAtMessageLimit = true,
                Error = "Limite de mensagens atingido. Esta foi a última mensagem permitida nesta conversa.",
                OperationMode = operationMode
            };
        }
        
        // Get AI response
        var history = conversation.Messages
            .OrderBy(m => m.Order)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id.ToString(),
                Role = m.Role,
                Content = m.Content,
                Timestamp = m.Timestamp,
                IsError = m.IsError
            })
            .ToList();
        
        // Add the new user message to history
        history.Add(new ChatMessageDto
        {
            Id = userMessage.Id.ToString(),
            Role = userMessage.Role,
            Content = userMessage.Content,
            Timestamp = userMessage.Timestamp,
            IsError = userMessage.IsError
        });
        
        try
        {
            var response = await _chatbotService.ProcessMessageAsync(
                message,
                history,
                userId,
                normalizedOperationMode,
                normalizedResponseStyle,
                false,
                conversationId,
                "conversation");
            
            // Create assistant message
            var assistantMessage = new ChatMessage
            {
                ConversationId = conversationId,
                Role = "assistant",
                Content = response.Response,
                Timestamp = DateTime.UtcNow,
                Order = nextOrder + 1,
                IsError = !response.Success
            };
            
            assistantMessage = await _conversationDao.AddMessageAsync(assistantMessage);
            currentCount++;
            
            return new ConversationMessageResponseDto
            {
                Success = response.Success,
                Error = response.Error,
                UserMessage = new ChatMessageDto
                {
                    Id = userMessage.Id.ToString(),
                    Role = userMessage.Role,
                    Content = userMessage.Content,
                    Timestamp = userMessage.Timestamp,
                    IsError = userMessage.IsError
                },
                AssistantMessage = new ChatMessageDto
                {
                    Id = assistantMessage.Id.ToString(),
                    Role = assistantMessage.Role,
                    Content = assistantMessage.Content,
                    Timestamp = assistantMessage.Timestamp,
                    IsError = assistantMessage.IsError
                },
                SuggestedActions = response.SuggestedActions,
                OperationMode = response.OperationMode,
                RequiresConfirmation = response.RequiresConfirmation,
                ConfirmationPrompt = response.ConfirmationPrompt,
                EvidenceSources = response.EvidenceSources,
                IsAtMessageLimit = currentCount >= ChatConversation.MaxMessages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chatbot message in conversation {ConversationId}", conversationId);
            
            // Create error message
            var errorMessage = new ChatMessage
            {
                ConversationId = conversationId,
                Role = "assistant",
                Content = "Desculpe, ocorreu um erro ao processar sua mensagem. Tente novamente.",
                Timestamp = DateTime.UtcNow,
                Order = nextOrder + 1,
                IsError = true
            };
            
            errorMessage = await _conversationDao.AddMessageAsync(errorMessage);
            currentCount++;
            
            return new ConversationMessageResponseDto
            {
                Success = false,
                Error = "Erro ao processar mensagem",
                UserMessage = new ChatMessageDto
                {
                    Id = userMessage.Id.ToString(),
                    Role = userMessage.Role,
                    Content = userMessage.Content,
                    Timestamp = userMessage.Timestamp,
                    IsError = userMessage.IsError
                },
                AssistantMessage = new ChatMessageDto
                {
                    Id = errorMessage.Id.ToString(),
                    Role = errorMessage.Role,
                    Content = errorMessage.Content,
                    Timestamp = errorMessage.Timestamp,
                    IsError = errorMessage.IsError
                },
                OperationMode = operationMode,
                IsAtMessageLimit = currentCount >= ChatConversation.MaxMessages
            };
        }
    }

    public async Task<bool> UpdateConversationAsync(int userId, int conversationId, UpdateConversationDto request)
    {
        var conversation = await _conversationDao.GetConversationAsync(conversationId);
        
        if (conversation == null || conversation.UserId != userId)
        {
            return false;
        }
        
        if (request.Title != null)
        {
            conversation.Title = request.Title.Length > MaxTitleLength 
                ? request.Title[..MaxTitleLength] 
                : request.Title;
        }
        
        if (request.IsArchived.HasValue)
        {
            conversation.IsArchived = request.IsArchived.Value;
        }

        if (request.OperationMode.HasValue)
        {
            conversation.DefaultOperationMode = (int)NormalizeOperationMode((int)request.OperationMode.Value);
        }

        if (request.ResponseStyle.HasValue)
        {
            conversation.DefaultResponseStyle = (int)NormalizeResponseStyle((int)request.ResponseStyle.Value);
        }
        
        await _conversationDao.UpdateConversationAsync(conversation);
        return true;
    }

    public async Task<bool> DeleteConversationAsync(int userId, int conversationId)
    {
        var conversation = await _conversationDao.GetConversationAsync(conversationId);
        
        if (conversation == null || conversation.UserId != userId)
        {
            return false;
        }
        
        await _conversationDao.DeleteConversationAsync(conversationId);
        return true;
    }
    
    /// <summary>
    /// Generate a conversation title from the first message.
    /// </summary>
    private string GenerateTitle(string firstMessage)
    {
        if (string.IsNullOrWhiteSpace(firstMessage))
        {
            return "Nova conversa";
        }
        
        // Clean up the message - remove newlines and extra spaces
        var cleaned = firstMessage
            .Replace("\r\n", " ")
            .Replace("\n", " ")
            .Replace("\r", " ");
        
        // Collapse multiple spaces
        while (cleaned.Contains("  "))
        {
            cleaned = cleaned.Replace("  ", " ");
        }
        
        cleaned = cleaned.Trim();
        
        // Truncate to max length with ellipsis
        if (cleaned.Length <= MaxTitleLength)
        {
            return cleaned;
        }
        
        // Try to cut at a word boundary
        var truncated = cleaned[..(MaxTitleLength - 3)];
        var lastSpace = truncated.LastIndexOf(' ');
        
        if (lastSpace > MaxTitleLength / 2)
        {
            truncated = truncated[..lastSpace];
        }
        
        return truncated + "...";
    }

    private static ChatOperationMode NormalizeOperationMode(int mode)
    {
        return Enum.IsDefined(typeof(ChatOperationMode), mode)
            ? (ChatOperationMode)mode
            : ChatOperationMode.ProposeAction;
    }

    private static ChatResponseStyle NormalizeResponseStyle(int style)
    {
        return Enum.IsDefined(typeof(ChatResponseStyle), style)
            ? (ChatResponseStyle)style
            : ChatResponseStyle.Executive;
    }
}
