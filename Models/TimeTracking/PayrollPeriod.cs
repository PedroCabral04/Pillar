using erp.Models.Audit;
using erp.Models.Identity;
using erp.Models.Payroll;

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

    public DateTime? CalculationDate { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int? ApprovedById { get; set; }
    public ApplicationUser? ApprovedBy { get; set; }

    public DateTime? PaidAt { get; set; }
    public int? PaidById { get; set; }
    public ApplicationUser? PaidBy { get; set; }

    public decimal TotalGrossAmount { get; set; }
    public decimal TotalNetAmount { get; set; }
    public decimal TotalInssAmount { get; set; }
    public decimal TotalIrrfAmount { get; set; }
    public decimal TotalEmployerCost { get; set; }

    public string? Notes { get; set; }

    public ICollection<PayrollEntry> Entries { get; set; } = new List<PayrollEntry>();
    public ICollection<PayrollResult> Results { get; set; } = new List<PayrollResult>();
}
