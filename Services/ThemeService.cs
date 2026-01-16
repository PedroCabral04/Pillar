using System;
using System.Threading.Tasks;
using MudBlazor;

namespace erp.Services;

public class ThemeService
{
    private readonly PreferenceService _preferenceService;
    
    private bool _isDarkMode;
    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode != value)
            {
                _isDarkMode = value;
                OnThemeChanged?.Invoke();
            }
        }
    }
    public MudTheme Theme { get; private set; } = new();
    public event Action? OnThemeChanged;

    public ThemeService(PreferenceService preferenceService)
    {
        _preferenceService = preferenceService;
        InitializeDefaultTheme();
        _preferenceService.OnPreferenceChanged += UpdateTheme;
    }

    public void InitializeTheme()
    {
        _isDarkMode = _preferenceService.CurrentPreferences.Ui.DarkMode;
        UpdateTheme();
    }

    private void InitializeDefaultTheme()
    {
        Theme = new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary = "#2563EB", // Modern Blue
                Secondary = "#475569", // Slate
                AppbarBackground = "#FFFFFF",
                AppbarText = "#1E293B",
                DrawerBackground = "#FFFFFF",
                DrawerText = "#334155",
                Surface = "#FFFFFF",
                Background = "#F8FAFC", // Very light slate
                TextPrimary = "#0F172A",
                TextSecondary = "#64748B",
                ActionDefault = "#64748B",
                LinesDefault = "#E2E8F0",
                TableLines = "#E2E8F0",
                Divider = "#E2E8F0",
                OverlayLight = "#1E293B80"
            },
            PaletteDark = new PaletteDark
            {
                Primary = "#3B82F6",
                Secondary = "#94A3B8",
                AppbarBackground = "#1E293B",
                AppbarText = "#F8FAFC",
                DrawerBackground = "#0F172A",
                DrawerText = "#CBD5E1",
                Surface = "#1E293B",
                Background = "#020617",
                TextPrimary = "#F8FAFC",
                TextSecondary = "#94A3B8",
                ActionDefault = "#94A3B8",
                LinesDefault = "#334155",
                TableLines = "#334155",
                Divider = "#334155",
                OverlayDark = "#00000080"
            },
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = "8px",
                DrawerWidthLeft = "280px",
                AppbarHeight = "64px"
            },
            Shadows = new Shadow()
        };

        UpdateTypography();
    }

    private void UpdateTypography()
    {
        Theme.Typography.Default.FontFamily = new[] { "Poppins", "Helvetica", "Arial", "sans-serif" };
        Theme.Typography.Default.FontSize = ".875rem";
        Theme.Typography.Default.FontWeight = "400";
        Theme.Typography.Default.LineHeight = "1.5";
        Theme.Typography.Default.LetterSpacing = ".00938em";

        Theme.Typography.H1.FontFamily = new[] { "Poppins", "sans-serif" };
        Theme.Typography.H1.FontWeight = "600";
        Theme.Typography.H1.FontSize = "2.25rem";
        Theme.Typography.H1.LineHeight = "1.167";

        Theme.Typography.H2.FontFamily = new[] { "Poppins", "sans-serif" };
        Theme.Typography.H2.FontWeight = "600";
        Theme.Typography.H2.FontSize = "1.875rem";
        Theme.Typography.H2.LineHeight = "1.2";

        Theme.Typography.H3.FontFamily = new[] { "Poppins", "sans-serif" };
        Theme.Typography.H3.FontWeight = "600";
        Theme.Typography.H3.FontSize = "1.5rem";
        Theme.Typography.H3.LineHeight = "1.167";

        Theme.Typography.H4.FontFamily = new[] { "Poppins", "sans-serif" };
        Theme.Typography.H4.FontWeight = "600";
        Theme.Typography.H4.FontSize = "1.25rem";
        Theme.Typography.H4.LineHeight = "1.235";

        Theme.Typography.H5.FontFamily = new[] { "Poppins", "sans-serif" };
        Theme.Typography.H5.FontWeight = "600";
        Theme.Typography.H5.FontSize = "1.125rem";
        Theme.Typography.H5.LineHeight = "1.334";

        Theme.Typography.H6.FontFamily = new[] { "Poppins", "sans-serif" };
        Theme.Typography.H6.FontWeight = "600";
        Theme.Typography.H6.FontSize = "1rem";
        Theme.Typography.H6.LineHeight = "1.6";

        Theme.Typography.Button.FontFamily = new[] { "Poppins", "sans-serif" };
        Theme.Typography.Button.FontWeight = "500";
        Theme.Typography.Button.FontSize = ".875rem";
        Theme.Typography.Button.LetterSpacing = ".02857em";
        Theme.Typography.Button.TextTransform = "none";
    }

    public async Task SetDarkModeAsync(bool value)
    {
        if (_isDarkMode != value)
        {
            _isDarkMode = value;
            _preferenceService.CurrentPreferences.Ui.DarkMode = value;
            await _preferenceService.SaveAsync();
            // UpdateTheme() será chamado via evento OnPreferenceChanged
        }
    }

    public void UpdatePrimaryColor(string color)
    {
        if (!string.IsNullOrEmpty(color))
        {
            Theme.PaletteLight.Primary = color;
            Theme.PaletteDark.Primary = color;
            OnThemeChanged?.Invoke();
        }
    }

    public void UpdateTheme()
    {
        // Apply Custom Primary Color first (before high contrast might override)
        var customColor = _preferenceService.CurrentPreferences.Ui.PrimaryColor;
        if (!string.IsNullOrEmpty(customColor) && customColor != "#2563EB")
        {
            Theme.PaletteLight.Primary = customColor;
            Theme.PaletteDark.Primary = customColor;
        }

        // Apply High Contrast
        if (_preferenceService.CurrentPreferences.Ui.HighContrast)
        {
            Theme.PaletteLight.Background = "#FFFFFF";
            Theme.PaletteLight.Surface = "#FFFFFF";
            Theme.PaletteLight.AppbarBackground = "#000000";
            Theme.PaletteLight.AppbarText = "#FFFFFF";
            Theme.PaletteLight.DrawerBackground = "#FFFFFF";
            Theme.PaletteLight.DrawerText = "#000000";
            Theme.PaletteLight.TextPrimary = "#000000";
            Theme.PaletteLight.TextSecondary = "#000000";
            Theme.PaletteLight.ActionDefault = "#000000";
            Theme.PaletteLight.LinesDefault = "#000000";
            Theme.PaletteLight.TableLines = "#000000";
            Theme.PaletteLight.Divider = "#000000";
            
            Theme.PaletteDark.Background = "#000000";
            Theme.PaletteDark.Surface = "#000000";
            Theme.PaletteDark.AppbarBackground = "#000000";
            Theme.PaletteDark.AppbarText = "#FFFFFF";
            Theme.PaletteDark.DrawerBackground = "#000000";
            Theme.PaletteDark.DrawerText = "#FFFFFF";
            Theme.PaletteDark.TextPrimary = "#FFFFFF";
            Theme.PaletteDark.TextSecondary = "#FFFFFF";
            Theme.PaletteDark.ActionDefault = "#FFFFFF";
            Theme.PaletteDark.LinesDefault = "#FFFFFF";
            Theme.PaletteDark.TableLines = "#FFFFFF";
            Theme.PaletteDark.Divider = "#FFFFFF";
        }
        else
        {
            // Restore default colors WITHOUT resetting PrimaryColor (which is user-configurable)
            Theme.PaletteLight.Background = "#F8FAFC";
            Theme.PaletteLight.Surface = "#FFFFFF";
            Theme.PaletteLight.AppbarBackground = "#FFFFFF";
            Theme.PaletteLight.AppbarText = "#1E293B";
            Theme.PaletteLight.DrawerBackground = "#FFFFFF";
            Theme.PaletteLight.DrawerText = "#334155";
            Theme.PaletteLight.TextPrimary = "#0F172A";
            Theme.PaletteLight.TextSecondary = "#64748B";
            Theme.PaletteLight.ActionDefault = "#64748B";
            Theme.PaletteLight.LinesDefault = "#E2E8F0";
            Theme.PaletteLight.TableLines = "#E2E8F0";
            Theme.PaletteLight.Divider = "#E2E8F0";

            Theme.PaletteDark.Background = "#020617";
            Theme.PaletteDark.Surface = "#1E293B";
            Theme.PaletteDark.AppbarBackground = "#1E293B";
            Theme.PaletteDark.AppbarText = "#F8FAFC";
            Theme.PaletteDark.DrawerBackground = "#0F172A";
            Theme.PaletteDark.DrawerText = "#CBD5E1";
            Theme.PaletteDark.TextPrimary = "#F8FAFC";
            Theme.PaletteDark.TextSecondary = "#94A3B8";
            Theme.PaletteDark.ActionDefault = "#94A3B8";
            Theme.PaletteDark.LinesDefault = "#334155";
            Theme.PaletteDark.TableLines = "#334155";
            Theme.PaletteDark.Divider = "#334155";

            // Re-apply custom primary color (it was preserved in the code above)
            if (!string.IsNullOrEmpty(customColor) && customColor != "#2563EB")
            {
                Theme.PaletteLight.Primary = customColor;
                Theme.PaletteDark.Primary = customColor;
            }
        }

        // Apply Font Size
        var fontSize = _preferenceService.CurrentPreferences.Ui.FontSize;
        double scale = fontSize switch
        {
            "small" => 0.85,
            "large" => 1.15,
            _ => 1.0
        };

        if (scale != 1.0)
        {
            Theme.Typography.Default.FontSize = $"{0.875 * scale:0.###}rem";
            Theme.Typography.H1.FontSize = $"{2.25 * scale:0.###}rem";
            Theme.Typography.H2.FontSize = $"{1.875 * scale:0.###}rem";
            Theme.Typography.H3.FontSize = $"{1.5 * scale:0.###}rem";
            Theme.Typography.H4.FontSize = $"{1.25 * scale:0.###}rem";
            Theme.Typography.H5.FontSize = $"{1.125 * scale:0.###}rem";
            Theme.Typography.H6.FontSize = $"{1.0 * scale:0.###}rem";
            Theme.Typography.Button.FontSize = $"{0.875 * scale:0.###}rem";
        }

        // Apply Density
        if (_preferenceService.CurrentPreferences.Ui.Density == "compact")
        {
            Theme.LayoutProperties.AppbarHeight = "48px";
            Theme.LayoutProperties.DrawerWidthLeft = "240px";
            Theme.LayoutProperties.DefaultBorderRadius = "4px";
        }
        else
        {
            Theme.LayoutProperties.AppbarHeight = "64px";
            Theme.LayoutProperties.DrawerWidthLeft = "280px";
            Theme.LayoutProperties.DefaultBorderRadius = "8px";
        }

        OnThemeChanged?.Invoke();
    }
}