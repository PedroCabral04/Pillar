using erp.Models.Tenancy;

namespace erp.Services.Tenancy;

public class TenantContext
{
    public int? TenantId { get; private set; }
    public string? Slug { get; private set; }
    public string? Name { get; private set; }
    public TenantStatus Status { get; private set; } = TenantStatus.Provisioning;
    public TenantBranding? Branding { get; private set; }
    public bool IsDemo { get; private set; }
    public bool IsResolved => TenantId.HasValue;

    internal void ApplyTenant(Tenant tenant)
    {
        TenantId = tenant.Id;
        Slug = tenant.Slug;
        Name = tenant.Name;
        Status = tenant.Status;
        Branding = tenant.Branding;
        IsDemo = tenant.IsDemo;
    }

    internal void Reset()
    {
        TenantId = null;
        Slug = null;
        Name = null;
        Branding = null;
        IsDemo = false;
        Status = TenantStatus.Provisioning;
    }
}
