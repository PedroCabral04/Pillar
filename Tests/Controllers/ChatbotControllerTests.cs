using System.Security.Claims;
using erp.Controllers;
using erp.DTOs.Chatbot;
using erp.Services.Chatbot;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace erp.Tests.Controllers;

public class ChatbotControllerTests
{
    private readonly Mock<IChatbotService> _chatbotService;
    private readonly Mock<IChatConversationService> _conversationService;
    private readonly Mock<IChatbotAuditService> _auditService;
    private readonly Mock<IChatbotCacheService> _cacheService;
    private readonly Mock<ILogger<ChatbotController>> _logger;
    private readonly ChatbotController _controller;

    public ChatbotControllerTests()
    {
        _chatbotService = new Mock<IChatbotService>();
        _conversationService = new Mock<IChatConversationService>();
        _auditService = new Mock<IChatbotAuditService>();
        _cacheService = new Mock<IChatbotCacheService>();
        _logger = new Mock<ILogger<ChatbotController>>();

        _controller = new ChatbotController(
            _chatbotService.Object,
            _conversationService.Object,
            _auditService.Object,
            _cacheService.Object,
            _logger.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "test@test.com")
            },
            "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task SendMessage_WithValidMessage_ReturnsOk()
    {
        var request = new ChatRequestDto
        {
            Message = "Oi",
            ConversationHistory = new List<ChatMessageDto>()
        };

        var expectedResponse = new ChatResponseDto
        {
            Success = true,
            Response = "Resposta"
        };

        _chatbotService
            .Setup(x => x.ProcessMessageAsync(
                request.Message,
                request.ConversationHistory,
                1,
                request.OperationMode,
                request.ResponseStyle,
                false,
                null,
                "quick"))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.SendMessage(request);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = okResult.Value.Should().BeOfType<ChatResponseDto>().Subject;
        payload.Success.Should().BeTrue();
        payload.Response.Should().Be("Resposta");
    }

    [Fact]
    public async Task SendMessage_WithWhitespaceMessage_ReturnsBadRequest()
    {
        var request = new ChatRequestDto { Message = "   " };

        var result = await _controller.SendMessage(request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetRecentAudit_ReturnsCurrentUserAuditEntries()
    {
        var logs = new List<ChatbotAuditEntryDto>
        {
            new() { Id = 1, Outcome = "processed", Source = "quick", Success = true }
        };

        _auditService
            .Setup(x => x.GetRecentByUserAsync(1, 20))
            .ReturnsAsync(logs);

        var result = await _controller.GetRecentAudit(20);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(logs);
    }

    [Fact]
    public void Health_ReturnsOk()
    {
        var result = _controller.Health();

        result.Should().BeOfType<OkObjectResult>();
    }
}
