using Microsoft.JSInterop;

namespace erp.Services.Browser;

/// <summary>
/// Implementation of browser interaction service using JSInterop
/// </summary>
public class BrowserService : IBrowserService
{
    private readonly IJSRuntime _jsRuntime;

    public BrowserService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<DeviceInfo> GetDeviceInfoAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<DeviceInfo>("erpResponsive.getDeviceInfo");
        }
        catch (JSException ex)
        {
            Console.WriteLine($"Error getting device info: {ex.Message}");
            // Return default desktop info on error
            return new DeviceInfo
            {
                Width = 1920,
                Height = 1080,
                IsDesktop = true,
                Breakpoint = "xl",
                Orientation = "landscape"
            };
        }
    }

    public async Task<bool> IsMobileAsync()
    {
        var deviceInfo = await GetDeviceInfoAsync();
        return deviceInfo.IsMobile;
    }

    public async Task<bool> IsTabletAsync()
    {
        var deviceInfo = await GetDeviceInfoAsync();
        return deviceInfo.IsTablet;
    }

    public async Task<bool> IsDesktopAsync()
    {
        var deviceInfo = await GetDeviceInfoAsync();
        return deviceInfo.IsDesktop;
    }

    public async Task<string> GetBreakpointAsync()
    {
        var deviceInfo = await GetDeviceInfoAsync();
        return deviceInfo.Breakpoint;
    }

    public async Task LockScrollAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("erpResponsive.lockScroll");
        }
        catch (JSException ex)
        {
            Console.WriteLine($"Error locking scroll: {ex.Message}");
        }
    }

    public async Task UnlockScrollAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("erpResponsive.unlockScroll");
        }
        catch (JSException ex)
        {
            Console.WriteLine($"Error unlocking scroll: {ex.Message}");
        }
    }

    public async Task<DotNetObjectReference<SwipeHandler>?> SetupSwipeGestureAsync(string elementId, SwipeHandler handler)
    {
        try
        {
            var handlerRef = DotNetObjectReference.Create(handler);
            
            var callbacks = new
            {
                onSwipeLeft = handler.OnSwipeLeft != null ? (Action?)(() => handler.InvokeSwipeLeft()) : null,
                onSwipeRight = handler.OnSwipeRight != null ? (Action?)(() => handler.InvokeSwipeRight()) : null,
                onSwipeUp = handler.OnSwipeUp != null ? (Action?)(() => handler.InvokeSwipeUp()) : null,
                onSwipeDown = handler.OnSwipeDown != null ? (Action?)(() => handler.InvokeSwipeDown()) : null
            };

            await _jsRuntime.InvokeVoidAsync("erpResponsive.setupSwipeGesture", elementId, callbacks);
            return handlerRef;
        }
        catch (JSException ex)
        {
            Console.WriteLine($"Error setting up swipe gesture: {ex.Message}");
            return null;
        }
    }

    public async Task AddTouchFeedbackAsync(string elementId)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("erpResponsive.addTouchFeedback", elementId);
        }
        catch (JSException ex)
        {
            Console.WriteLine($"Error adding touch feedback: {ex.Message}");
        }
    }

    public async Task VibrateAsync(int milliseconds)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("erpResponsive.vibrate", milliseconds);
        }
        catch (JSException ex)
        {
            Console.WriteLine($"Error triggering vibration: {ex.Message}");
        }
    }

    public async Task VibrateAsync(int[] pattern)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("erpResponsive.vibrate", pattern);
        }
        catch (JSException ex)
        {
            Console.WriteLine($"Error triggering vibration pattern: {ex.Message}");
        }
    }
}
