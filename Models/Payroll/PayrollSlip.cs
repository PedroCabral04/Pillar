using System.ComponentModel.DataAnnotations;
using erp.Models.Audit;
using erp.Models.Identity;

namespace erp.Models.Payroll;

public class PayrollSlip : IAuditable
{
    public int Id { get; set; }

    public int PayrollResultId { get; set; }
    public PayrollResult PayrollResult { get; set; } = null!;

    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(64)]
    public string FileHash { get; set; } = string.Empty;

    [MaxLength(120)]
    public string ContentType { get; set; } = "application/pdf";

    public long FileSize { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public int GeneratedById { get; set; }
    public ApplicationUser GeneratedBy { get; set; } = null!;

    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedById { get; set; }
    public ApplicationUser? UpdatedBy { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
