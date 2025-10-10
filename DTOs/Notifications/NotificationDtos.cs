namespace erp.DTOs.Notifications;

public class Notification
{
    public required string Id { get; set; }
    public required string UserId { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public NotificationType Type { get; set; } = NotificationType.Info;
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ActionUrl { get; set; }
    public string? ActionText { get; set; }
    public string? Category { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error,
    System
}

public enum NotificationPriority
{
    Low,
    Normal,
    High,
    Urgent
}

public class NotificationPreferences
{
    public bool EnableEmailNotifications { get; set; } = true;
    public bool EnablePushNotifications { get; set; } = true;
    public bool EnableInAppNotifications { get; set; } = true;
    public bool EnableSoundNotifications { get; set; } = true;
    public List<string> MutedCategories { get; set; } = new();
    public Dictionary<string, bool> CategoryPreferences { get; set; } = new();
    public TimeSpan? QuietHoursStart { get; set; }
    public TimeSpan? QuietHoursEnd { get; set; }
}

public class NotificationFilter
{
    public bool? IsRead { get; set; }
    public NotificationType? Type { get; set; }
    public NotificationPriority? Priority { get; set; }
    public string? Category { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
}

public class NotificationSummary
{
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public int UrgentCount { get; set; }
    public Dictionary<NotificationType, int> CountByType { get; set; } = new();
    public Dictionary<string, int> CountByCategory { get; set; } = new();
}

public class CreateNotificationRequest
{
    public required string UserId { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public NotificationType Type { get; set; } = NotificationType.Info;
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public string? ActionUrl { get; set; }
    public string? ActionText { get; set; }
    public string? Category { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class BulkNotificationRequest
{
    public required List<string> UserIds { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public NotificationType Type { get; set; } = NotificationType.Info;
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public string? ActionUrl { get; set; }
    public string? ActionText { get; set; }
    public string? Category { get; set; }
}

public class MarkAsReadRequest
{
    public List<string>? NotificationIds { get; set; }
    public bool MarkAll { get; set; }
}
