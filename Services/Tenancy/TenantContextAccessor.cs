using erp.Models.Tenancy;

namespace erp.Services.Tenancy;

public class TenantContextAccessor : ITenantContextAccessor
{
    private readonly TenantContext _current = new();

    public TenantContext Current => _current;

    public void SetTenant(Tenant tenant)
    {
        _current.ApplyTenant(tenant);
    }

    public void Clear()
    {
        _current.Reset();
    }
}
