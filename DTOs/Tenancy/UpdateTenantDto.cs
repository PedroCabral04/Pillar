using erp.Models.Tenancy;
using System.ComponentModel.DataAnnotations;

namespace erp.DTOs.Tenancy;

public class UpdateTenantDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? DocumentNumber { get; set; }

    [MaxLength(200)]
    public string? PrimaryContactName { get; set; }

    [MaxLength(200)]
    [EmailAddress]
    public string? PrimaryContactEmail { get; set; }

    [MaxLength(20)]
    public string? PrimaryContactPhone { get; set; }

    [MaxLength(200)]
    public string? Region { get; set; }

    public bool IsDemo { get; set; }

    public TenantStatus Status { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public TenantBrandingDto? Branding { get; set; }
}
