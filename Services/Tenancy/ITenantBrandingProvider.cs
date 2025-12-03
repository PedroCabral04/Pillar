namespace erp.Services.Tenancy;

public interface ITenantBrandingProvider
{
    TenantBrandingTheme GetCurrentBranding();
    
    /// <summary>
    /// Notifica que o branding foi alterado e deve ser recarregado.
    /// </summary>
    event EventHandler? BrandingChanged;
    
    /// <summary>
    /// Dispara o evento de BrandingChanged para forçar atualização da UI.
    /// </summary>
    void NotifyBrandingChanged();
}
