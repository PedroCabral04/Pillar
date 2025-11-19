namespace erp.Services.Tenancy;

public interface ITenantContextAccessor
{
    TenantContext Current { get; }
    void SetTenant(Models.Tenancy.Tenant tenant);
    void Clear();
}
