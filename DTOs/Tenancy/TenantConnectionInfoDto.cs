using erp.Models.Tenancy;

namespace erp.DTOs.Tenancy;

public record TenantConnectionInfoDto(
    int Id,
    string Name,
    string Slug,
    TenantStatus Status,
    string? DatabaseName,
    string? ConnectionString,
    DateTime CreatedAt,
    DateTime? ActivatedAt
);
