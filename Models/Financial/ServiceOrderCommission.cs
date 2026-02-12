using erp.Models.Audit;
using erp.Models.Identity;
using erp.Models.Payroll;
using erp.Models.ServiceOrders;
using Microsoft.EntityFrameworkCore;

namespace erp.Models.Financial;

/// <summary>
/// Commission earned by a service technician/seller from a service order
/// </summary>
public class ServiceOrderCommission : IAuditable, IMustHaveTenant
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    // Service Order reference
    public int ServiceOrderId { get; set; }
    public virtual ServiceOrder ServiceOrder { get; set; } = null!;

    // Service Order item reference
    public int ServiceOrderItemId { get; set; }
    public virtual ServiceOrderItem ServiceOrderItem { get; set; } = null!;

    // Salesperson who earned the commission
    public int UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    // Commission calculation
    [Precision(18, 2)]
    public decimal ProfitAmount { get; set; }
    [Precision(5, 2)]
    public decimal CommissionPercent { get; set; }
    [Precision(18, 2)]
    public decimal CommissionAmount { get; set; }

    public CommissionStatus Status { get; set; } = CommissionStatus.Pending;
    public DateTime? PaidDate { get; set; }
    public int? PayrollId { get; set; }
    public virtual PayrollResult? Payroll { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CreatedByUserId { get; set; }
    public virtual ApplicationUser CreatedByUser { get; set; } = null!;
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedByUserId { get; set; }
    public virtual ApplicationUser? UpdatedByUser { get; set; }
}
