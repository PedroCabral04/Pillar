using System.Text.Json;
using erp.DAOs.Chatbot;
using erp.DTOs.Chatbot;
using erp.Models.Chatbot;
using erp.Services.Tenancy;

namespace erp.Services.Chatbot;

public class ChatbotAuditService : IChatbotAuditService
{
    private readonly IChatbotAuditDao _auditDao;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ILogger<ChatbotAuditService> _logger;

    public ChatbotAuditService(
        IChatbotAuditDao auditDao,
        ITenantContextAccessor tenantContextAccessor,
        ILogger<ChatbotAuditService> logger)
    {
        _auditDao = auditDao;
        _tenantContextAccessor = tenantContextAccessor;
        _logger = logger;
    }

    public async Task LogAsync(ChatbotAuditRequest request)
    {
        try
        {
            var response = request.Response;
            var entry = new ChatbotAuditEntry
            {
                CreatedAt = DateTime.UtcNow,
                UserId = request.UserId,
                TenantId = _tenantContextAccessor.Current?.TenantId,
                ConversationId = request.ConversationId,
                Source = Truncate(request.Source, 30) ?? "quick",
                Outcome = Truncate(request.Outcome, 60) ?? "unknown",
                RequestMessage = Truncate(request.RequestMessage, 4000) ?? string.Empty,
                EffectiveMessage = Truncate(request.EffectiveMessage, 4000) ?? string.Empty,
                ResponseMessage = Truncate(response?.Response, 8000),
                Error = Truncate(response?.Error, 1000),
                OperationMode = (int)request.OperationMode,
                ResponseStyle = (int)request.ResponseStyle,
                IsConfirmedAction = request.IsConfirmedAction,
                RequiresConfirmation = response?.RequiresConfirmation ?? false,
                Success = response?.Success ?? false,
                SuggestedActionsJson = SerializeSafe(response?.SuggestedActions),
                EvidenceSourcesJson = SerializeSafe(response?.EvidenceSources),
                AiProvider = Truncate(request.AiProvider, 30),
                AiConfigured = request.AiConfigured,
                DurationMs = request.DurationMs
            };

            await _auditDao.AddAsync(entry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write chatbot audit entry");
        }
    }

    public async Task<List<ChatbotAuditEntryDto>> GetRecentByUserAsync(int userId, int take = 30)
    {
        var logs = await _auditDao.GetRecentByUserAsync(userId, take);

        return logs.Select(log => new ChatbotAuditEntryDto
        {
            Id = log.Id,
            CreatedAt = log.CreatedAt,
            ConversationId = log.ConversationId,
            Source = log.Source,
            Outcome = log.Outcome,
            Success = log.Success,
            RequiresConfirmation = log.RequiresConfirmation,
            OperationMode = Enum.IsDefined(typeof(ChatOperationMode), log.OperationMode)
                ? (ChatOperationMode)log.OperationMode
                : ChatOperationMode.ProposeAction,
            ResponseStyle = Enum.IsDefined(typeof(ChatResponseStyle), log.ResponseStyle)
                ? (ChatResponseStyle)log.ResponseStyle
                : ChatResponseStyle.Executive,
            DurationMs = log.DurationMs,
            RequestMessagePreview = Truncate(log.RequestMessage, 120) ?? string.Empty,
            Error = log.Error
        }).ToList();
    }

    private static string? SerializeSafe<T>(T value)
    {
        if (value == null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Serialize(value);
        }
        catch
        {
            return null;
        }
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value.Length <= maxLength
            ? value
            : value[..maxLength];
    }
}
