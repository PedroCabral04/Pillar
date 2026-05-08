using System.ComponentModel.DataAnnotations;
using erp.Models.Audit;
using erp.Models.Identity;
using erp.Models.ServiceOrders;
using Microsoft.EntityFrameworkCore;

namespace erp.Models.Financial;

public class EmployeePayment : IAuditable, IMustHaveTenant
{
    public int Id { get; set; }

    public int TenantId { get; set; }

    public int EmployeeId { get; set; }
    public virtual ApplicationUser Employee { get; set; } = null!;

    public EmployeePaymentType Type { get; set; }

    public AccountStatus Status { get; set; } = AccountStatus.Pending;

    [Precision(18, 2)]
    public decimal Amount { get; set; }

    [Precision(18, 2)]
    public decimal DiscountAmount { get; set; }

    [Precision(18, 2)]
    public decimal NetAmount { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public int ReferenceMonth { get; set; }
    public int ReferenceYear { get; set; }

    public DateTime DueDate { get; set; }
    public DateTime? PaymentDate { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Pix;

    public int? CategoryId { get; set; }
    public virtual FinancialCategory? Category { get; set; }

    public int? CostCenterId { get; set; }
    public virtual CostCenter? CostCenter { get; set; }

    public int? ServiceOrderId { get; set; }
    public virtual ServiceOrder? ServiceOrder { get; set; }

    public int? ServiceCount { get; set; }

    [Precision(18, 2)]
    public decimal? ServiceUnitValue { get; set; }

    [Precision(5, 2)]
    public decimal? ServicePercent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public int CreatedByUserId { get; set; }
    public virtual ApplicationUser? CreatedByUser { get; set; }

    public int? PaidByUserId { get; set; }
    public virtual ApplicationUser? PaidByUser { get; set; }
}
