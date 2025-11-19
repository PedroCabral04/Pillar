namespace erp.Services.Tenancy;

public interface ITenantBrandingProvider
{
    TenantBrandingTheme GetCurrentBranding();
}
