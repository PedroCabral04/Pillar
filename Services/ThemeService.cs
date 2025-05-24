using MudBlazor.Interfaces;


namespace erp.Services;

public class ThemeService
{
    public bool IsDarkMode { get; set; } 
    public event Action? OnThemeChanged;

    public void SetDarkMode(bool value)
    {
        if (IsDarkMode != value)
        {
            IsDarkMode = value;
            OnThemeChanged?.Invoke();
        }
    }
    
}