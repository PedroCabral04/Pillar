using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using erp.Services.Notifications;
using erp.DTOs.Notifications;
using System.Security.Claims;

namespace erp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IAdvancedNotificationService _notificationService;

    public NotificationsController(IAdvancedNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<ActionResult<List<Notification>>> GetNotifications([FromQuery] NotificationFilter filter)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var notifications = await _notificationService.GetUserNotificationsAsync(userId, filter);
        return Ok(notifications);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Notification>> GetNotification(string id)
    {
        var notification = await _notificationService.GetNotificationAsync(id);
        if (notification == null)
        {
            return NotFound(new { error = "Notification not found" });
        }

        // Verify ownership
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (notification.UserId != userId)
        {
            return Forbid();
        }

        return Ok(notification);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<NotificationSummary>> GetSummary()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var summary = await _notificationService.GetSummaryAsync(userId);
        return Ok(summary);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Notification>> CreateNotification([FromBody] CreateNotificationRequest request)
    {
        var notification = await _notificationService.CreateNotificationAsync(request);
        return CreatedAtAction(nameof(GetNotification), new { id = notification.Id }, notification);
    }

    [HttpPost("bulk")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<Notification>>> CreateBulkNotifications([FromBody] BulkNotificationRequest request)
    {
        var notifications = await _notificationService.CreateBulkNotificationsAsync(request);
        return Ok(notifications);
    }

    [HttpPatch("{id}/read")]
    public async Task<ActionResult> MarkAsRead(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var success = await _notificationService.MarkAsReadAsync(userId, id);
        if (!success)
        {
            return NotFound(new { error = "Notification not found" });
        }

        return Ok(new { message = "Notification marked as read" });
    }

    [HttpPost("mark-all-read")]
    public async Task<ActionResult> MarkAllAsRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var count = await _notificationService.MarkAllAsReadAsync(userId);
        return Ok(new { message = $"{count} notifications marked as read" });
    }

    [HttpPost("mark-multiple-read")]
    public async Task<ActionResult> MarkMultipleAsRead([FromBody] MarkAsReadRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (request.MarkAll)
        {
            var count = await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(new { message = $"{count} notifications marked as read" });
        }

        if (request.NotificationIds == null || !request.NotificationIds.Any())
        {
            return BadRequest(new { error = "NotificationIds required when MarkAll is false" });
        }

        var markedCount = await _notificationService.MarkMultipleAsReadAsync(userId, request.NotificationIds);
        return Ok(new { message = $"{markedCount} notifications marked as read" });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteNotification(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var success = await _notificationService.DeleteNotificationAsync(userId, id);
        if (!success)
        {
            return NotFound(new { error = "Notification not found" });
        }

        return Ok(new { message = "Notification deleted successfully" });
    }

    [HttpDelete("read/all")]
    public async Task<ActionResult> DeleteAllRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var count = await _notificationService.DeleteAllReadAsync(userId);
        return Ok(new { message = $"{count} read notifications deleted" });
    }

    [HttpGet("preferences")]
    public async Task<ActionResult<NotificationPreferences>> GetPreferences()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var preferences = await _notificationService.GetPreferencesAsync(userId);
        return Ok(preferences);
    }

    [HttpPost("preferences")]
    public async Task<ActionResult> SavePreferences([FromBody] NotificationPreferences preferences)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await _notificationService.SavePreferencesAsync(userId, preferences);
        return Ok(new { message = "Preferences saved successfully" });
    }
}
