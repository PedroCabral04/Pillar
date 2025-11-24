using System.ComponentModel.DataAnnotations;

namespace erp.DTOs.Tenancy;

public class CreateTenantDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    [RegularExpression("^[a-z0-9-]+$", ErrorMessage = "Slug deve conter apenas letras minúsculas, números e hífen")]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? DocumentNumber { get; set; }

    [MaxLength(200)]
    [EmailAddress]
    public string? PrimaryContactEmail { get; set; }

    [MaxLength(200)]
    public string? PrimaryContactName { get; set; }

    [MaxLength(20)]
    public string? PrimaryContactPhone { get; set; }

    [MaxLength(200)]
    public string? Region { get; set; }

    public bool IsDemo { get; set; }

    public TenantBrandingDto? Branding { get; set; }

    [MaxLength(200)]
    public string? DatabaseName { get; set; }

    [MaxLength(500)]
    public string? ConnectionString { get; set; }

    public bool ProvisionDatabase { get; set; }
}
