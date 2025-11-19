using System;
using System.Linq;

namespace erp.Services.Tenancy;

public record class TenantBrandingTheme
{
    public static TenantBrandingTheme Default { get; } = new();

    public int? TenantId { get; init; }
    public string? TenantSlug { get; init; }
    public string TenantName { get; init; } = "Pillar ERP";
    public string PrimaryColor { get; init; } = "#594AE2";
    public string SecondaryColor { get; init; } = "#00ACC1";
    public string AccentColor { get; init; } = "#FFC107";
    public string BackgroundColor { get; init; } = "#ECEFF1";
    public string SurfaceColor { get; init; } = "#FFFFFF";
    public string TextPrimary { get; init; } = "#263238";
    public string TextSecondary { get; init; } = "#607D8B";
    public string? LogoUrl { get; init; }
    public string? FaviconUrl { get; init; } = "/favicon.ico";
    public string? LoginBackgroundUrl { get; init; }
    public string? CustomCss { get; init; }

    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(TenantName))
            {
                return "P";
            }

            var parts = TenantName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Take(2)
                .Select(p => char.ToUpperInvariant(p[0]));

            return string.Concat(parts);
        }
    }
}
