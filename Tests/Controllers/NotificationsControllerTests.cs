using System.Security.Claims;
using erp.Controllers;
using erp.DTOs.Notifications;
using erp.Services.Notifications;
using Microsoft.AspNetCore.Http;

namespace erp.Tests.Controllers;

/// <summary>
/// Testes unitários para o controlador de notificações
/// </summary>
public class NotificationsControllerTests
{
    private readonly Mock<IAdvancedNotificationService> _mockNotificationService;
    private readonly Mock<ILogger<NotificationsController>> _mockLogger;
    private readonly NotificationsController _controller;
    private readonly string _testUserId = "1";

    public NotificationsControllerTests()
    {
        _mockNotificationService = new Mock<IAdvancedNotificationService>();
        _mockLogger = new Mock<ILogger<NotificationsController>>();

        _controller = new NotificationsController(_mockNotificationService.Object);

        // Setup user context
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId),
            new Claim(ClaimTypes.Name, "test@test.com"),
            new Claim(ClaimTypes.Role, "User")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    #region GetNotifications Tests

    [Fact]
    public async Task GetNotifications_ReturnsOkWithNotificationList()
    {
        // Arrange
        var filter = new NotificationFilter();
        var notifications = new List<Notification>
        {
            new Notification { Id = "1", Title = "Test 1", Message = "Test message 1", UserId = _testUserId, IsRead = false },
            new Notification { Id = "2", Title = "Test 2", Message = "Test message 2", UserId = _testUserId, IsRead = false }
        };

        _mockNotificationService
            .Setup(x => x.GetUserNotificationsAsync(_testUserId, filter))
            .ReturnsAsync(notifications);

        // Act
        var result = await _controller.GetNotifications(filter);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedNotifications = okResult.Value as List<Notification>;
        returnedNotifications.Should().NotBeNull();
        returnedNotifications.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetNotifications_WithoutUserId_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();
        var filter = new NotificationFilter();

        // Act
        var result = await _controller.GetNotifications(filter);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    #endregion

    #region GetNotification Tests

    [Fact]
    public async Task GetNotification_WithExistingId_ReturnsOk()
    {
        // Arrange
        var notificationId = "1";
        var notification = new Notification
        {
            Id = notificationId,
            Title = "Test Notification",
            Message = "Test message",
            UserId = _testUserId,
            IsRead = false
        };

        _mockNotificationService
            .Setup(x => x.GetNotificationAsync(notificationId))
            .ReturnsAsync(notification);

        // Act
        var result = await _controller.GetNotification(notificationId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedNotification = okResult.Value as Notification;
        returnedNotification.Should().NotBeNull();
        returnedNotification!.Id.Should().Be(notificationId);
    }

    [Fact]
    public async Task GetNotification_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var notificationId = "999";
        _mockNotificationService
            .Setup(x => x.GetNotificationAsync(notificationId))
            .ReturnsAsync((Notification?)null);

        // Act
        var result = await _controller.GetNotification(notificationId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetNotification_WithDifferentUserId_ReturnsForbid()
    {
        // Arrange
        var notificationId = "1";
        var notification = new Notification
        {
            Id = notificationId,
            Title = "Test Notification",
            Message = "Test message",
            UserId = "999", // Different user
            IsRead = false
        };

        _mockNotificationService
            .Setup(x => x.GetNotificationAsync(notificationId))
            .ReturnsAsync(notification);

        // Act
        var result = await _controller.GetNotification(notificationId);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region GetSummary Tests

    [Fact]
    public async Task GetSummary_ReturnsOkWithSummary()
    {
        // Arrange
        var summary = new NotificationSummary
        {
            TotalCount = 10,
            UnreadCount = 5
        };

        _mockNotificationService
            .Setup(x => x.GetSummaryAsync(_testUserId))
            .ReturnsAsync(summary);

        // Act
        var result = await _controller.GetSummary();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSummary = okResult.Value as NotificationSummary;
        returnedSummary.Should().NotBeNull();
        returnedSummary!.TotalCount.Should().Be(10);
        returnedSummary.UnreadCount.Should().Be(5);
    }

    [Fact]
    public async Task GetSummary_WithoutUserId_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

        // Act
        var result = await _controller.GetSummary();

        // Assert
        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    #endregion

    #region MarkAsRead Tests

    [Fact]
    public async Task MarkAsRead_WithValidId_ReturnsOk()
    {
        // Arrange
        var notificationId = "1";
        _mockNotificationService
            .Setup(x => x.MarkAsReadAsync(_testUserId, notificationId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.MarkAsRead(notificationId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task MarkAsRead_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var notificationId = "999";
        _mockNotificationService
            .Setup(x => x.MarkAsReadAsync(_testUserId, notificationId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.MarkAsRead(notificationId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task MarkAllAsRead_ReturnsOkWithCount()
    {
        // Arrange
        var markedCount = 5;
        _mockNotificationService
            .Setup(x => x.MarkAllAsReadAsync(_testUserId))
            .ReturnsAsync(markedCount);

        // Act
        var result = await _controller.MarkAllAsRead();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task MarkMultipleAsRead_WithMarkAll_ReturnsOk()
    {
        // Arrange
        var request = new MarkAsReadRequest { MarkAll = true };
        _mockNotificationService
            .Setup(x => x.MarkAllAsReadAsync(_testUserId))
            .ReturnsAsync(5);

        // Act
        var result = await _controller.MarkMultipleAsRead(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task MarkMultipleAsRead_WithIds_ReturnsOk()
    {
        // Arrange
        var request = new MarkAsReadRequest
        {
            MarkAll = false,
            NotificationIds = new List<string> { "1", "2", "3" }
        };

        _mockNotificationService
            .Setup(x => x.MarkMultipleAsReadAsync(_testUserId, request.NotificationIds))
            .ReturnsAsync(3);

        // Act
        var result = await _controller.MarkMultipleAsRead(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task MarkMultipleAsRead_WithoutIdsAndMarkAllFalse_ReturnsBadRequest()
    {
        // Arrange
        var request = new MarkAsReadRequest
        {
            MarkAll = false,
            NotificationIds = null
        };

        // Act
        var result = await _controller.MarkMultipleAsRead(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task DeleteNotification_WithValidId_ReturnsOk()
    {
        // Arrange
        var notificationId = "1";
        _mockNotificationService
            .Setup(x => x.DeleteNotificationAsync(_testUserId, notificationId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteNotification(notificationId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeleteNotification_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var notificationId = "999";
        _mockNotificationService
            .Setup(x => x.DeleteNotificationAsync(_testUserId, notificationId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteNotification(notificationId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteAllRead_ReturnsOkWithCount()
    {
        // Arrange
        var deletedCount = 3;
        _mockNotificationService
            .Setup(x => x.DeleteAllReadAsync(_testUserId))
            .ReturnsAsync(deletedCount);

        // Act
        var result = await _controller.DeleteAllRead();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region Preferences Tests

    [Fact]
    public async Task GetPreferences_ReturnsOkWithPreferences()
    {
        // Arrange
        var preferences = new NotificationPreferences
        {
            EnableEmailNotifications = true,
            EnablePushNotifications = false
        };

        _mockNotificationService
            .Setup(x => x.GetPreferencesAsync(_testUserId))
            .ReturnsAsync(preferences);

        // Act
        var result = await _controller.GetPreferences();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedPrefs = okResult.Value as NotificationPreferences;
        returnedPrefs.Should().NotBeNull();
        returnedPrefs!.EnableEmailNotifications.Should().BeTrue();
        returnedPrefs.EnablePushNotifications.Should().BeFalse();
    }

    [Fact]
    public async Task SavePreferences_WithValidData_ReturnsOk()
    {
        // Arrange
        var preferences = new NotificationPreferences
        {
            EnableEmailNotifications = true,
            EnablePushNotifications = true
        };

        _mockNotificationService
            .Setup(x => x.SavePreferencesAsync(_testUserId, preferences))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.SavePreferences(preferences);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockNotificationService.Verify(x => x.SavePreferencesAsync(_testUserId, preferences), Times.Once);
    }

    #endregion
}
