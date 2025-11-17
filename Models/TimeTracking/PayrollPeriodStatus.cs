namespace erp.Models.TimeTracking;

/// <summary>
/// Status do apontamento de horas.
/// </summary>
public enum PayrollPeriodStatus
{
    Draft = 0,
    Calculated = 1,
    Approved = 2,
    Paid = 3,
    Locked = 4
}
