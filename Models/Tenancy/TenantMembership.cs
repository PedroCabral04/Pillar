using erp.Models.Audit;
using erp.Models.Identity;
using System.ComponentModel.DataAnnotations;

namespace erp.Models.Tenancy;

public class TenantMembership : IAuditable
{
    public int Id { get; set; }

    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public bool IsDefault { get; set; }

    [MaxLength(100)]
    public string? AssignedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
}
