using Microsoft.JSInterop;

namespace erp.Services.Browser;

/// <summary>
/// Service for interacting with browser capabilities and responsive state
/// </summary>
public interface IBrowserService
{
    /// <summary>
    /// Get current device information including breakpoint, dimensions, and capabilities
    /// </summary>
    Task<DeviceInfo> GetDeviceInfoAsync();

    /// <summary>
    /// Check if current viewport is mobile (< 600px)
    /// </summary>
    Task<bool> IsMobileAsync();

    /// <summary>
    /// Check if current viewport is tablet (600-959px)
    /// </summary>
    Task<bool> IsTabletAsync();

    /// <summary>
    /// Check if current viewport is desktop (>= 960px)
    /// </summary>
    Task<bool> IsDesktopAsync();

    /// <summary>
    /// Get current breakpoint name (xs, sm, md, lg, xl)
    /// </summary>
    Task<string> GetBreakpointAsync();

    /// <summary>
    /// Lock body scroll (useful for modals on mobile)
    /// </summary>
    Task LockScrollAsync();

    /// <summary>
    /// Unlock body scroll
    /// </summary>
    Task UnlockScrollAsync();

    /// <summary>
    /// Setup swipe gesture handler on an element
    /// </summary>
    Task<DotNetObjectReference<SwipeHandler>?> SetupSwipeGestureAsync(string elementId, SwipeHandler handler);

    /// <summary>
    /// Add touch feedback to an element
    /// </summary>
    Task AddTouchFeedbackAsync(string elementId);

    /// <summary>
    /// Trigger vibration (if supported)
    /// </summary>
    Task VibrateAsync(int milliseconds);

    /// <summary>
    /// Trigger vibration pattern (if supported)
    /// </summary>
    Task VibrateAsync(int[] pattern);
}

/// <summary>
/// Device information from browser
/// </summary>
public class DeviceInfo
{
    public int Width { get; set; }
    public int Height { get; set; }
    public bool IsIOS { get; set; }
    public bool IsAndroid { get; set; }
    public bool IsMobile { get; set; }
    public bool IsTablet { get; set; }
    public bool IsDesktop { get; set; }
    public bool IsTouchDevice { get; set; }
    public bool IsStandalone { get; set; }
    public string Breakpoint { get; set; } = "xs";
    public string Orientation { get; set; } = "portrait";
}

/// <summary>
/// Handler for swipe gestures
/// </summary>
public class SwipeHandler
{
    public Action? OnSwipeLeft { get; set; }
    public Action? OnSwipeRight { get; set; }
    public Action? OnSwipeUp { get; set; }
    public Action? OnSwipeDown { get; set; }

    [JSInvokable]
    public void InvokeSwipeLeft() => OnSwipeLeft?.Invoke();

    [JSInvokable]
    public void InvokeSwipeRight() => OnSwipeRight?.Invoke();

    [JSInvokable]
    public void InvokeSwipeUp() => OnSwipeUp?.Invoke();

    [JSInvokable]
    public void InvokeSwipeDown() => OnSwipeDown?.Invoke();
}
