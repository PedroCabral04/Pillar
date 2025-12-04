using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using erp.DTOs.Chatbot;
using erp.Services.Chatbot;
using System.Security.Claims;

namespace erp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatbotController : ControllerBase
{
    private readonly IChatbotService _chatbotService;
    private readonly IChatConversationService _conversationService;
    private readonly ILogger<ChatbotController> _logger;

    public ChatbotController(
        IChatbotService chatbotService,
        IChatConversationService conversationService,
        ILogger<ChatbotController> logger)
    {
        _chatbotService = chatbotService;
        _conversationService = conversationService;
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
    /// Send a message without persisting (widget quick mode).
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
                request.ConversationHistory);

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
    /// Get all conversations for the current user.
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
    /// Get a specific conversation with all messages.
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
    /// Create a new conversation.
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
    /// Send a message in a conversation.
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
        
        var response = await _conversationService.SendMessageAsync(userId.Value, id, request.Message);
        
        if (!response.Success && response.Error == "Conversa não encontrada")
        {
            return NotFound(response);
        }
        
        return Ok(response);
    }
    
    /// <summary>
    /// Update conversation (title, archive status).
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
    /// Delete a conversation.
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
}
