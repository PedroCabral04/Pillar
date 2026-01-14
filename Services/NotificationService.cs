using MudBlazor;

namespace erp.Services;

/// <summary>
/// Simple UI notification service for transient toast/snackbar messages.
/// </summary>
/// <remarks>
/// This service provides quick, ephemeral notifications that appear and disappear automatically.
/// For persistent notifications with read/unread status, preferences, and history,
/// use <see cref="Notifications.IAdvancedNotificationService"/> instead.
/// </remarks>
public interface INotificationService
{
    void ShowSuccess(string message);
    void ShowError(string message);
    void ShowWarning(string message);
    void ShowInfo(string message);
}

/// <summary>
/// Implementation of transient UI notifications using MudBlazor Snackbar.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ISnackbar _snackbar;

    public NotificationService(ISnackbar snackbar)
    {
        _snackbar = snackbar;
    }

    public void ShowSuccess(string message)
    {
        _snackbar.Add($"✓ {message}", Severity.Success, config =>
        {
            config.VisibleStateDuration = 3000;
            config.HideTransitionDuration = 500;
            config.ShowTransitionDuration = 500;
            config.SnackbarVariant = Variant.Filled;
        });
    }

    public void ShowError(string message)
    {
        _snackbar.Add($"✕ {message}", Severity.Error, config =>
        {
            config.VisibleStateDuration = 5000;
            config.HideTransitionDuration = 500;
            config.ShowTransitionDuration = 500;
            config.SnackbarVariant = Variant.Filled;
        });
    }

    public void ShowWarning(string message)
    {
        _snackbar.Add($"⚠ {message}", Severity.Warning, config =>
        {
            config.VisibleStateDuration = 4000;
            config.HideTransitionDuration = 500;
            config.ShowTransitionDuration = 500;
            config.SnackbarVariant = Variant.Filled;
        });
    }

    public void ShowInfo(string message)
    {
        _snackbar.Add($"ℹ {message}", Severity.Info, config =>
        {
            config.VisibleStateDuration = 3000;
            config.HideTransitionDuration = 500;
            config.ShowTransitionDuration = 500;
            config.SnackbarVariant = Variant.Filled;
        });
    }
}
