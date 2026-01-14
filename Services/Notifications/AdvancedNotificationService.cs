using erp.DTOs.Notifications;

namespace erp.Services.Notifications;

/// <summary>
/// Advanced notification service for persistent notifications with full lifecycle management.
/// </summary>
/// <remarks>
/// This service provides a comprehensive notification system with:
/// - Persistent storage (in-memory for now, can be extended to database)
/// - Read/unread tracking and preferences
/// - Filtering, pagination, and expiration
/// - Real-time events for notification center UI
///
/// For simple transient toast notifications, use <see cref="Services.INotificationService"/> instead.
/// </remarks>
public interface IAdvancedNotificationService
{
    // Notification CRUD
    Task<Notification> CreateNotificationAsync(CreateNotificationRequest request);
    Task<List<Notification>> CreateBulkNotificationsAsync(BulkNotificationRequest request);
    Task<Notification?> GetNotificationAsync(string notificationId);
    Task<List<Notification>> GetUserNotificationsAsync(string userId, NotificationFilter filter);
    Task<bool> MarkAsReadAsync(string userId, string notificationId);
    Task<int> MarkAllAsReadAsync(string userId);
    Task<int> MarkMultipleAsReadAsync(string userId, List<string> notificationIds);
    Task<bool> DeleteNotificationAsync(string userId, string notificationId);
    Task<int> DeleteAllReadAsync(string userId);
    Task<int> DeleteExpiredAsync();

    // Preferences
    Task<NotificationPreferences> GetPreferencesAsync(string userId);
    Task SavePreferencesAsync(string userId, NotificationPreferences preferences);

    // Summary & Stats
    Task<NotificationSummary> GetSummaryAsync(string userId);

    // Real-time events
    event EventHandler<Notification>? OnNotificationCreated;
    event EventHandler<string>? OnNotificationRead;
    event EventHandler<string>? OnNotificationDeleted;
}

public class AdvancedNotificationService : IAdvancedNotificationService
{
    private readonly Dictionary<string, List<Notification>> _notificationStore = new();
    private readonly Dictionary<string, NotificationPreferences> _preferencesStore = new();
    
    public event EventHandler<Notification>? OnNotificationCreated;
    public event EventHandler<string>? OnNotificationRead;
    public event EventHandler<string>? OnNotificationDeleted;

    public Task<Notification> CreateNotificationAsync(CreateNotificationRequest request)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = request.UserId,
            Title = request.Title,
            Message = request.Message,
            Type = request.Type,
            Priority = request.Priority,
            ActionUrl = request.ActionUrl,
            ActionText = request.ActionText,
            Category = request.Category,
            ExpiresAt = request.ExpiresAt,
            Metadata = request.Metadata,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        if (!_notificationStore.ContainsKey(request.UserId))
        {
            _notificationStore[request.UserId] = new List<Notification>();
        }

        _notificationStore[request.UserId].Add(notification);
        OnNotificationCreated?.Invoke(this, notification);

        return Task.FromResult(notification);
    }

    public async Task<List<Notification>> CreateBulkNotificationsAsync(BulkNotificationRequest request)
    {
        var notifications = new List<Notification>();

        foreach (var userId in request.UserIds)
        {
            var notification = await CreateNotificationAsync(new CreateNotificationRequest
            {
                UserId = userId,
                Title = request.Title,
                Message = request.Message,
                Type = request.Type,
                Priority = request.Priority,
                ActionUrl = request.ActionUrl,
                ActionText = request.ActionText,
                Category = request.Category
            });

            notifications.Add(notification);
        }

        return notifications;
    }

    public Task<Notification?> GetNotificationAsync(string notificationId)
    {
        foreach (var notifications in _notificationStore.Values)
        {
            var notification = notifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification != null)
            {
                return Task.FromResult<Notification?>(notification);
            }
        }

        return Task.FromResult<Notification?>(null);
    }

    public Task<List<Notification>> GetUserNotificationsAsync(string userId, NotificationFilter filter)
    {
        if (!_notificationStore.TryGetValue(userId, out var notifications))
        {
            return Task.FromResult(new List<Notification>());
        }

        var query = notifications.AsEnumerable();

        // Apply filters
        if (filter.IsRead.HasValue)
        {
            query = query.Where(n => n.IsRead == filter.IsRead.Value);
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(n => n.Type == filter.Type.Value);
        }

        if (filter.Priority.HasValue)
        {
            query = query.Where(n => n.Priority == filter.Priority.Value);
        }

        if (!string.IsNullOrEmpty(filter.Category))
        {
            query = query.Where(n => n.Category == filter.Category);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(n => n.CreatedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(n => n.CreatedAt <= filter.ToDate.Value);
        }

        // Filter out expired
        query = query.Where(n => !n.ExpiresAt.HasValue || n.ExpiresAt.Value > DateTime.UtcNow);

        // Order by priority and date
        query = query.OrderByDescending(n => n.Priority)
                     .ThenByDescending(n => n.CreatedAt);

        // Pagination
        query = query.Skip(filter.Skip).Take(filter.Take);

        return Task.FromResult(query.ToList());
    }

    public Task<bool> MarkAsReadAsync(string userId, string notificationId)
    {
        if (!_notificationStore.TryGetValue(userId, out var notifications))
        {
            return Task.FromResult(false);
        }

        var notification = notifications.FirstOrDefault(n => n.Id == notificationId);
        if (notification == null || notification.IsRead)
        {
            return Task.FromResult(false);
        }

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        OnNotificationRead?.Invoke(this, notificationId);

        return Task.FromResult(true);
    }

    public Task<int> MarkAllAsReadAsync(string userId)
    {
        if (!_notificationStore.TryGetValue(userId, out var notifications))
        {
            return Task.FromResult(0);
        }

        var count = 0;
        foreach (var notification in notifications.Where(n => !n.IsRead))
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            OnNotificationRead?.Invoke(this, notification.Id);
            count++;
        }

        return Task.FromResult(count);
    }

    public Task<int> MarkMultipleAsReadAsync(string userId, List<string> notificationIds)
    {
        if (!_notificationStore.TryGetValue(userId, out var notifications))
        {
            return Task.FromResult(0);
        }

        var count = 0;
        foreach (var id in notificationIds)
        {
            var notification = notifications.FirstOrDefault(n => n.Id == id);
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                OnNotificationRead?.Invoke(this, id);
                count++;
            }
        }

        return Task.FromResult(count);
    }

    public Task<bool> DeleteNotificationAsync(string userId, string notificationId)
    {
        if (!_notificationStore.TryGetValue(userId, out var notifications))
        {
            return Task.FromResult(false);
        }

        var notification = notifications.FirstOrDefault(n => n.Id == notificationId);
        if (notification == null)
        {
            return Task.FromResult(false);
        }

        notifications.Remove(notification);
        OnNotificationDeleted?.Invoke(this, notificationId);

        return Task.FromResult(true);
    }

    public Task<int> DeleteAllReadAsync(string userId)
    {
        if (!_notificationStore.TryGetValue(userId, out var notifications))
        {
            return Task.FromResult(0);
        }

        var toRemove = notifications.Where(n => n.IsRead).ToList();
        foreach (var notification in toRemove)
        {
            notifications.Remove(notification);
            OnNotificationDeleted?.Invoke(this, notification.Id);
        }

        return Task.FromResult(toRemove.Count);
    }

    public Task<int> DeleteExpiredAsync()
    {
        var count = 0;
        var now = DateTime.UtcNow;

        foreach (var (userId, notifications) in _notificationStore)
        {
            var expired = notifications.Where(n => n.ExpiresAt.HasValue && n.ExpiresAt.Value <= now).ToList();
            foreach (var notification in expired)
            {
                notifications.Remove(notification);
                OnNotificationDeleted?.Invoke(this, notification.Id);
                count++;
            }
        }

        return Task.FromResult(count);
    }

    public Task<NotificationPreferences> GetPreferencesAsync(string userId)
    {
        if (!_preferencesStore.TryGetValue(userId, out var preferences))
        {
            preferences = new NotificationPreferences();
            _preferencesStore[userId] = preferences;
        }

        return Task.FromResult(preferences);
    }

    public Task SavePreferencesAsync(string userId, NotificationPreferences preferences)
    {
        _preferencesStore[userId] = preferences;
        return Task.CompletedTask;
    }

    public Task<NotificationSummary> GetSummaryAsync(string userId)
    {
        if (!_notificationStore.TryGetValue(userId, out var notifications))
        {
            return Task.FromResult(new NotificationSummary());
        }

        // Filter out expired
        var activeNotifications = notifications
            .Where(n => !n.ExpiresAt.HasValue || n.ExpiresAt.Value > DateTime.UtcNow)
            .ToList();

        var summary = new NotificationSummary
        {
            TotalCount = activeNotifications.Count,
            UnreadCount = activeNotifications.Count(n => !n.IsRead),
            UrgentCount = activeNotifications.Count(n => n.Priority == NotificationPriority.Urgent && !n.IsRead),
            CountByType = activeNotifications
                .GroupBy(n => n.Type)
                .ToDictionary(g => g.Key, g => g.Count()),
            CountByCategory = activeNotifications
                .Where(n => !string.IsNullOrEmpty(n.Category))
                .GroupBy(n => n.Category!)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return Task.FromResult(summary);
    }
}
