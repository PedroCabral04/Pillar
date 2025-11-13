using System.Security.Claims;
using erp.Controllers;
using erp.DTOs.Chatbot;
using erp.Services.Chatbot;
using Microsoft.AspNetCore.Http;

namespace erp.Tests.Controllers;

/// <summary>
/// Testes unitários para o controlador de Chatbot
/// </summary>
public class ChatbotControllerTests
{
    private readonly Mock<IChatbotService> _mockChatbotService;
    private readonly Mock<ILogger<ChatbotController>> _mockLogger;
    private readonly ChatbotController _controller;

    public ChatbotControllerTests()
    {
        _mockChatbotService = new Mock<IChatbotService>();
        _mockLogger = new Mock<ILogger<ChatbotController>>();

        _controller = new ChatbotController(_mockChatbotService.Object, _mockLogger.Object);

        // Setup user context
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "test@test.com")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    #region SendMessage Tests

    [Fact]
    public async Task SendMessage_WithValidMessage_ReturnsOk()
    {
        // Arrange
        var request = new ChatRequestDto
        {
            Message = "Hello, how can you help me?",
            ConversationHistory = new List<ChatMessageDto>()
        };

        var expectedResponse = new ChatResponseDto
        {
            Success = true,
            Response = "I can help you with various tasks in the ERP system."
        };

        _mockChatbotService
            .Setup(x => x.ProcessMessageAsync(request.Message, request.ConversationHistory))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SendMessage(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value as ChatResponseDto;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Response.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SendMessage_WithEmptyMessage_ReturnsBadRequest()
    {
        // Arrange
        var request = new ChatRequestDto
        {
            Message = "",
            ConversationHistory = new List<ChatMessageDto>()
        };

        // Act
        var result = await _controller.SendMessage(request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value as ChatResponseDto;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.Error.Should().Be("Mensagem não pode estar vazia");
    }

    [Fact]
    public async Task SendMessage_WithNullMessage_ReturnsBadRequest()
    {
        // Arrange
        var request = new ChatRequestDto
        {
            Message = null!,
            ConversationHistory = new List<ChatMessageDto>()
        };

        // Act
        var result = await _controller.SendMessage(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SendMessage_WithConversationHistory_ReturnsOk()
    {
        // Arrange
        var conversationHistory = new List<ChatMessageDto>
        {
            new ChatMessageDto { Role = "user", Content = "Previous question" },
            new ChatMessageDto { Role = "assistant", Content = "Previous answer" }
        };

        var request = new ChatRequestDto
        {
            Message = "Follow-up question",
            ConversationHistory = conversationHistory
        };

        var expectedResponse = new ChatResponseDto
        {
            Success = true,
            Response = "Here's your answer based on context."
        };

        _mockChatbotService
            .Setup(x => x.ProcessMessageAsync(request.Message, request.ConversationHistory))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SendMessage(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value as ChatResponseDto;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SendMessage_WithServiceException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new ChatRequestDto
        {
            Message = "Test message",
            ConversationHistory = new List<ChatMessageDto>()
        };

        _mockChatbotService
            .Setup(x => x.ProcessMessageAsync(request.Message, request.ConversationHistory))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.SendMessage(request);

        // Assert
        var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        var response = statusCodeResult.Value as ChatResponseDto;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.Error.Should().Be("Erro interno ao processar mensagem");
    }

    [Fact]
    public async Task SendMessage_WithWhitespaceMessage_ReturnsBadRequest()
    {
        // Arrange
        var request = new ChatRequestDto
        {
            Message = "   ",
            ConversationHistory = new List<ChatMessageDto>()
        };

        // Act
        var result = await _controller.SendMessage(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Health Tests

    [Fact]
    public void Health_ReturnsOkWithStatus()
    {
        // Act
        var result = _controller.Health();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    #endregion
}
