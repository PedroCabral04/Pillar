using MudBlazor;
using Microsoft.JSInterop;

namespace erp.Services;

public interface INotificationService
{
    void ShowSuccess(string message);
    void ShowError(string message);
    void ShowWarning(string message);
    void ShowInfo(string message);
    Task PlayNotificationSoundAsync();
}

public class NotificationService : INotificationService
{
    private readonly ISnackbar _snackbar;
    private readonly IJSRuntime _jsRuntime;
    private readonly PreferenceService _preferenceService;

    public NotificationService(ISnackbar snackbar, IJSRuntime jsRuntime, PreferenceService preferenceService)
    {
        _snackbar = snackbar;
        _jsRuntime = jsRuntime;
        _preferenceService = preferenceService;
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
        _ = PlayNotificationSoundIfEnabledAsync();
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
        _ = PlayNotificationSoundIfEnabledAsync();
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
        _ = PlayNotificationSoundIfEnabledAsync();
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
        _ = PlayNotificationSoundIfEnabledAsync();
    }

    private async Task PlayNotificationSoundIfEnabledAsync()
    {
        try
        {
            var prefs = _preferenceService.CurrentPreferences.Notifications;
            if (prefs.Sounds && prefs.InApp)
            {
                await PlayNotificationSoundAsync();
            }
        }
        catch
        {
            // Ignore errors playing sound
        }
    }

    public async Task PlayNotificationSoundAsync()
    {
        try
        {
            var prefs = _preferenceService.CurrentPreferences.Notifications;
            var volume = prefs.Volume / 100.0;
            var soundFile = prefs.SoundFile ?? "notification.mp3";
            await _jsRuntime.InvokeVoidAsync("erpNotifications.playSound", $"/sounds/{soundFile}", volume);
        }
        catch
        {
            // Ignore errors - sound playback is optional
        }
    }
}
