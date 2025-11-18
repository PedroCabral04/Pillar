using erp.Services.Tenancy;
using MudBlazor;

namespace erp.Components.Branding;

public static class TenantBrandingThemeBuilder
{
    public static MudTheme BuildTheme(TenantBrandingTheme branding)
    {
        return new MudTheme
        {
            PaletteLight = BuildLightPalette(branding),
            PaletteDark = BuildDarkPalette(branding),
            Typography = new Typography
            {
                Default = new DefaultTypography
                {
                    FontFamily = new[] { "Poppins", "Roboto", "Segoe UI", "Arial", "sans-serif" },
                },
                H5 = new H5Typography { FontWeight = "600", LetterSpacing = "0.2px" },
                Button = new ButtonTypography { TextTransform = "none", FontWeight = "600" },
                Subtitle2 = new Subtitle2Typography { FontWeight = "600" }
            }
        };
    }

    private static PaletteLight BuildLightPalette(TenantBrandingTheme branding) => new()
    {
        Primary = branding.PrimaryColor,
        Secondary = branding.SecondaryColor,
        Tertiary = branding.AccentColor,
        Success = Colors.Green.Darken2,
        Info = Colors.LightBlue.Darken1,
        Warning = Colors.Orange.Darken1,
        Error = Colors.Red.Darken1,
        Background = Colors.BlueGray.Lighten5,
        Surface = Colors.Gray.Lighten5,
        AppbarBackground = branding.PrimaryColor,
        DrawerBackground = Colors.BlueGray.Lighten4,
        TextPrimary = branding.TextPrimary,
        TextSecondary = branding.TextSecondary,
    };

    private static PaletteDark BuildDarkPalette(TenantBrandingTheme branding) => new()
    {
        Primary = branding.PrimaryColor,
        Secondary = branding.SecondaryColor,
        Tertiary = branding.AccentColor,
        Success = Colors.Green.Lighten2,
        Info = Colors.LightBlue.Lighten2,
        Warning = Colors.Orange.Lighten2,
        Error = Colors.Red.Lighten2,
        Background = Colors.BlueGray.Darken4,
        Surface = Colors.BlueGray.Darken3,
        AppbarBackground = branding.PrimaryColor,
        DrawerBackground = Colors.BlueGray.Darken3,
        TextPrimary = Colors.Gray.Lighten5,
        TextSecondary = Colors.Gray.Lighten2,
    };
}
