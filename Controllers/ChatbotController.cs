using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using erp.DTOs.Chatbot;
using erp.Services.Chatbot;
using System.Security.Claims;

namespace erp.Controllers;

[ApiController]
[Route("api/chatbot")]
[Route("api/assistente")]
[Authorize]
public class ChatbotController : ControllerBase
{
    private readonly IChatbotService _chatbotService;
    private readonly IChatConversationService _conversationService;
    private readonly IChatbotAuditService _auditService;
    private readonly IChatbotCacheService _cacheService;
    private readonly ILogger<ChatbotController> _logger;

    public ChatbotController(
        IChatbotService chatbotService,
        IChatConversationService conversationService,
        IChatbotAuditService auditService,
        IChatbotCacheService cacheService,
        ILogger<ChatbotController> logger)
    {
        _chatbotService = chatbotService;
        _conversationService = conversationService;
        _auditService = auditService;
        _cacheService = cacheService;
        _logger = logger;
    }
    
    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }

    /// <summary>
    /// Enviar uma mensagem sem persistir (modo rápido do widget).
    /// </summary>
    [HttpPost("message")]
    public async Task<ActionResult<ChatResponseDto>> SendMessage([FromBody] ChatRequestDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new ChatResponseDto
                {
                    Success = false,
                    Error = "Mensagem não pode estar vazia"
                });
            }

            var response = await _chatbotService.ProcessMessageAsync(
                request.Message,
                request.ConversationHistory,
                GetCurrentUserId(),
                request.OperationMode,
                request.ResponseStyle,
                false,
                null,
                "quick");

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar mensagem do chatbot");
            return StatusCode(500, new ChatResponseDto
            {
                Success = false,
                Error = "Erro interno ao processar mensagem"
            });
        }
    }

    /// <summary>
    /// Obter todas as conversas do usuário atual.
    /// </summary>
    [HttpGet("conversations")]
    public async Task<ActionResult<List<ChatConversationListDto>>> GetConversations([FromQuery] bool includeArchived = false)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }
        
        var conversations = await _conversationService.GetUserConversationsAsync(userId.Value, includeArchived);
        return Ok(conversations);
    }
    
    /// <summary>
    /// Obter uma conversa específica com todas as mensagens.
    /// </summary>
    [HttpGet("conversations/{id}")]
    public async Task<ActionResult<ChatConversationDto>> GetConversation(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }
        
        var conversation = await _conversationService.GetConversationAsync(userId.Value, id);
        if (conversation == null)
        {
            return NotFound();
        }
        
        return Ok(conversation);
    }
    
    /// <summary>
    /// Criar uma nova conversa.
    /// </summary>
    [HttpPost("conversations")]
    public async Task<ActionResult<ChatConversationDto>> CreateConversation([FromBody] CreateConversationDto request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }
        
        var conversation = await _conversationService.CreateConversationAsync(userId.Value, request);
        return CreatedAtAction(nameof(GetConversation), new { id = conversation.Id }, conversation);
    }
    
    /// <summary>
    /// Enviar uma mensagem em uma conversa.
    /// </summary>
    [HttpPost("conversations/{id}/messages")]
    public async Task<ActionResult<ConversationMessageResponseDto>> SendMessageToConversation(int id, [FromBody] SendMessageToConversationDto request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }
        
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new ConversationMessageResponseDto
            {
                Success = false,
                Error = "Mensagem não pode estar vazia"
            });
        }
        
        var response = await _conversationService.SendMessageAsync(
            userId.Value,
            id,
            request.Message,
            request.OperationMode,
            request.ResponseStyle);
        
        if (!response.Success && response.Error == "Conversa não encontrada")
        {
            return NotFound(response);
        }
        
        return Ok(response);
    }
    
    /// <summary>
    /// Atualizar conversa (título, estado de arquivamento).
    /// </summary>
    [HttpPut("conversations/{id}")]
    public async Task<ActionResult> UpdateConversation(int id, [FromBody] UpdateConversationDto request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }
        
        var success = await _conversationService.UpdateConversationAsync(userId.Value, id, request);
        if (!success)
        {
            return NotFound();
        }
        
        return NoContent();
    }
    
    /// <summary>
    /// Excluir uma conversa.
    /// </summary>
    [HttpDelete("conversations/{id}")]
    public async Task<ActionResult> DeleteConversation(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }
        
        var success = await _conversationService.DeleteConversationAsync(userId.Value, id);
        if (!success)
        {
            return NotFound();
        }
        
        return NoContent();
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", service = "chatbot" });
    }

    /// <summary>
    /// Obter trilha de auditoria recente do chatbot para o usuário atual.
    /// </summary>
    [HttpGet("audit/recent")]
    public async Task<ActionResult<List<ChatbotAuditEntryDto>>> GetRecentAudit([FromQuery] int take = 30)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var normalizedTake = Math.Clamp(take, 1, 100);
        var logs = await _auditService.GetRecentByUserAsync(userId.Value, normalizedTake);
        return Ok(logs);
    }

    /// <summary>
    /// Obter estatísticas de cache para monitoramento e análise de custos.
    /// </summary>
    [HttpGet("cache/stats")]
    public ActionResult<ChatCacheStatistics> GetCacheStats()
    {
        var stats = _cacheService.GetStatistics();
        return Ok(new
        {
            enabled = _cacheService.IsEnabled,
            statistics = stats,
            summary = new
            {
                responseCacheHitRate = $"{stats.ResponseHitRate:F1}%",
                pluginCacheHitRate = $"{stats.PluginHitRate:F1}%",
                estimatedApiCallsSaved = stats.EstimatedApiCallsSaved
            }
        });
    }

    /// <summary>
    /// Invalidar caches de plugins (útil após alterações em massa nos dados).
    /// </summary>
    [HttpPost("cache/invalidate")]
    public IActionResult InvalidateCache([FromQuery] string? pluginName = null)
    {
        if (string.IsNullOrEmpty(pluginName))
        {
            _cacheService.InvalidatePluginCache("ProductsPlugin");
            _cacheService.InvalidatePluginCache("SalesPlugin");
            _cacheService.InvalidatePluginCache("FinancialPlugin");
            _cacheService.InvalidatePluginCache("HRPlugin");
            //_cacheService.InvalidatePluginCache("AssetsPlugin");
            _cacheService.InvalidatePluginCache("CustomersPlugin");
            _cacheService.InvalidatePluginCache("SuppliersPlugin");
            _cacheService.InvalidatePluginCache("PayrollPlugin");
            
            _logger.LogInformation("Cache de todos os plugins invalidado por requisição");
            return Ok(new { message = "Cache de todos os plugins invalidado" });
        }
        
        _cacheService.InvalidatePluginCache(pluginName);
        _logger.LogInformation("Cache do plugin {PluginName} invalidado por requisição", pluginName);
        return Ok(new { message = $"Cache do plugin {pluginName} invalidado" });
    }
}
