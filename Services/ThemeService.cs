using System;
using System.Threading.Tasks;

namespace erp.Services;

public class ThemeService
{
    private readonly PreferenceService _preferenceService;
    
    public bool IsDarkMode { get; set; }
    public event Action? OnThemeChanged;

    public ThemeService(PreferenceService preferenceService)
    {
        _preferenceService = preferenceService;
    }

    public void InitializeTheme()
    {
        IsDarkMode = _preferenceService.CurrentPreferences.Ui.DarkMode;
        OnThemeChanged?.Invoke();
    }

    public async Task SetDarkModeAsync(bool value)
    {
        if (IsDarkMode != value)
        {
            IsDarkMode = value;
            _preferenceService.CurrentPreferences.Ui.DarkMode = value;
            await _preferenceService.SaveAsync();
            OnThemeChanged?.Invoke();
        }
    }
}