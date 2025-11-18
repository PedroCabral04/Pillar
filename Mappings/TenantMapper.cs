using Riok.Mapperly.Abstractions;
using erp.Models.Tenancy;
using erp.DTOs.Tenancy;
using System.Collections.Generic;

namespace erp.Mappings;

[Mapper]
public partial class TenantMapper
{
    public partial TenantDto TenantToTenantDto(Tenant tenant);
    public partial IEnumerable<TenantDto> TenantsToTenantDtos(IEnumerable<Tenant> tenants);
    public partial TenantConnectionInfoDto TenantToConnectionInfoDto(Tenant tenant);

    public partial Tenant CreateTenantDtoToTenant(CreateTenantDto dto);

    public partial void UpdateTenantFromDto(UpdateTenantDto dto, Tenant tenant);

    public partial TenantBrandingDto? TenantBrandingToDto(TenantBranding? branding);
    public partial TenantBranding? TenantBrandingDtoToEntity(TenantBrandingDto? dto);
}
