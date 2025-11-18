using erp.Models.Audit;
using erp.Models.Identity;
using System.ComponentModel.DataAnnotations;

namespace erp.Models.Tenancy;

public class Tenant : IAuditable
{
    public int Id { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(64)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? DocumentNumber { get; set; }

    [MaxLength(200)]
    public string? PrimaryContactName { get; set; }

    [MaxLength(200)]
    public string? PrimaryContactEmail { get; set; }

    [MaxLength(20)]
    public string? PrimaryContactPhone { get; set; }

    public TenantStatus Status { get; set; } = TenantStatus.Provisioning;

    [MaxLength(200)]
    public string? DatabaseName { get; set; }

    [MaxLength(500)]
    public string? ConnectionString { get; set; }

    [MaxLength(200)]
    public string? Region { get; set; }

    public bool IsDemo { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ActivatedAt { get; set; }
    public DateTime? SuspendedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public int? BrandingId { get; set; }
    public TenantBranding? Branding { get; set; }

    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    public ICollection<TenantMembership> Memberships { get; set; } = new List<TenantMembership>();
}
