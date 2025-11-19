namespace erp.DTOs.Tenancy;

public record TenantBrandingDto(
    string? LogoUrl,
    string? FaviconUrl,
    string? PrimaryColor,
    string? SecondaryColor,
    string? AccentColor,
    string? LoginBackgroundUrl,
    string? EmailFooterHtml,
    string? CustomCss
);
