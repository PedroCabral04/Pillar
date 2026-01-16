using erp.Models.Tenancy;

namespace erp.DTOs.Tenancy;

public record TenantDto(
    int Id,
    string Name,
    string Slug,
    TenantStatus Status,
    string? DocumentNumber,
    string? PrimaryContactName,
    string? PrimaryContactEmail,
    string? PrimaryContactPhone,
    string? Region,
    bool IsDemo,
    DateTime CreatedAt,
    DateTime? ActivatedAt,
    DateTime? SuspendedAt,
    string? Notes,
    TenantBrandingDto? Branding
);
