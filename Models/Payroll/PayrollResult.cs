using System.ComponentModel.DataAnnotations;
using erp.Models.Audit;
using erp.Models.Identity;
using erp.Models.TimeTracking;

namespace erp.Models.Payroll;

/// <summary>
/// Resultado consolidado de um colaborador em um período de folha.
/// </summary>
public class PayrollResult : IAuditable
{
    public int Id { get; set; }

    public int PayrollPeriodId { get; set; }
    public PayrollPeriod PayrollPeriod { get; set; } = null!;

    public int EmployeeId { get; set; }
    public ApplicationUser Employee { get; set; } = null!;

    public int? PayrollEntryId { get; set; }
    public PayrollEntry? PayrollEntry { get; set; }

    [MaxLength(200)]
    public string EmployeeNameSnapshot { get; set; } = string.Empty;

    [MaxLength(14)]
    public string? EmployeeCpfSnapshot { get; set; }

    [MaxLength(100)]
    public string? DepartmentSnapshot { get; set; }

    [MaxLength(100)]
    public string? PositionSnapshot { get; set; }

    [MaxLength(100)]
    public string? BankNameSnapshot { get; set; }

    [MaxLength(10)]
    public string? BankAgencySnapshot { get; set; }

    [MaxLength(20)]
    public string? BankAccountSnapshot { get; set; }

    public int DependentsSnapshot { get; set; }

    [Range(typeof(decimal), "0", "99999999")]
    public decimal BaseSalarySnapshot { get; set; }

    public decimal TotalEarnings { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalContributions { get; set; }
    public decimal NetAmount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal InssAmount { get; set; }
    public decimal IrrfAmount { get; set; }
    public decimal? AdditionalEmployerCost { get; set; }

    public DateTime? PaymentDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedById { get; set; }
    public ApplicationUser? UpdatedBy { get; set; }

    public ICollection<PayrollComponent> Components { get; set; } = new List<PayrollComponent>();
    public PayrollSlip? Slip { get; set; }
}
