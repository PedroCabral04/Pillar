using erp.Models.Audit;
using erp.Models.Identity;

namespace erp.Models.TimeTracking;

/// <summary>
/// Valores de apontamento de horas para um colaborador específico dentro de um período.
/// </summary>
public class PayrollEntry : IAuditable
{
    public int Id { get; set; }

    public int PayrollPeriodId { get; set; }
    public PayrollPeriod PayrollPeriod { get; set; } = null!;

    public int EmployeeId { get; set; }
    public ApplicationUser Employee { get; set; } = null!;

    public decimal? Faltas { get; set; }
    public decimal? Abonos { get; set; }
    public decimal? HorasExtras { get; set; }
    public decimal? Atrasos { get; set; }
    public string? Observacoes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedById { get; set; }
    public ApplicationUser? UpdatedBy { get; set; }
}
