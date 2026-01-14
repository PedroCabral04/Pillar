using erp.Models.Tenancy;

namespace erp.Services.Tenancy;

public class TenantBrandingProvider : ITenantBrandingProvider
{
    private readonly ITenantContextAccessor _tenantContextAccessor;

    public TenantBrandingProvider(ITenantContextAccessor tenantContextAccessor)
    {
        _tenantContextAccessor = tenantContextAccessor;
    }

    public event EventHandler? BrandingChanged;

    public void NotifyBrandingChanged()
    {
        BrandingChanged?.Invoke(this, EventArgs.Empty);
    }

    public TenantBrandingTheme GetCurrentBranding()
    {
        var context = _tenantContextAccessor.Current;
        var branding = context.Branding;

        return TenantBrandingTheme.Default with
        {
            TenantId = context.TenantId,
            TenantSlug = context.Slug,
            TenantName = string.IsNullOrWhiteSpace(context.Name)
                ? TenantBrandingTheme.Default.TenantName
                : context.Name!,
            PrimaryColor = NormalizeColor(branding?.PrimaryColor, TenantBrandingTheme.Default.PrimaryColor),
            SecondaryColor = NormalizeColor(branding?.SecondaryColor, TenantBrandingTheme.Default.SecondaryColor),
            AccentColor = NormalizeColor(branding?.AccentColor, TenantBrandingTheme.Default.AccentColor),
            LogoUrl = NormalizeUrl(branding?.LogoUrl),
            FaviconUrl = NormalizeUrl(branding?.FaviconUrl) ?? TenantBrandingTheme.Default.FaviconUrl,
            LoginBackgroundUrl = NormalizeUrl(branding?.LoginBackgroundUrl),
            CustomCss = branding?.CustomCss,
            UpdatedAt = branding?.UpdatedAt ?? branding?.CreatedAt,
        };
    }

    private static string NormalizeColor(string? color, string fallback)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return fallback;
        }

        color = color.Trim();
        if (color.StartsWith("var(", StringComparison.OrdinalIgnoreCase) ||
            color.StartsWith("rgb", StringComparison.OrdinalIgnoreCase) ||
            color.StartsWith("hsl", StringComparison.OrdinalIgnoreCase))
        {
            return color;
        }

        if (!color.StartsWith('#'))
        {
            if (color.Length is 3 or 6)
            {
                return $"#{color}";
            }
        }

        return color;
    }

    private static string? NormalizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        return url.Trim();
    }
}
