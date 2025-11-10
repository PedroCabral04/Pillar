using erp.Models.Audit;
using erp.Models.Identity;

namespace erp.Models.TimeTracking;

/// <summary>
/// Representa o apontamento de horas de um período específico.
/// </summary>
public class PayrollPeriod : IAuditable
{
    public int Id { get; set; }
    public int ReferenceMonth { get; set; }
    public int ReferenceYear { get; set; }
    public PayrollPeriodStatus Status { get; set; } = PayrollPeriodStatus.Draft;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedById { get; set; }
    public ApplicationUser? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedById { get; set; }
    public ApplicationUser? UpdatedBy { get; set; }

    public ICollection<PayrollEntry> Entries { get; set; } = new List<PayrollEntry>();
}
