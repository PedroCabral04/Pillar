using erp.Models.Audit;
using erp.Models.Identity;
using erp.Models.Payroll;
using erp.Models.Sales;

namespace erp.Models.Financial;

/// <summary>
/// Represents a commission earned by a salesperson from a sale
/// </summary>
public class Commission : IAuditable, IMustHaveTenant
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    // Sale reference
    public int SaleId { get; set; }
    public virtual Sale Sale { get; set; } = null!;

    // Sale item reference (for detailed tracking)
    public int SaleItemId { get; set; }
    public virtual SaleItem SaleItem { get; set; } = null!;

    // Product reference (for reporting)
    public int ProductId { get; set; }
    public virtual Inventory.Product Product { get; set; } = null!;

    // Salesperson who earned the commission
    public int UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    // Commission calculation
    public decimal ProfitAmount { get; set; } // (SalePrice - CostPrice) * Quantity
    public decimal CommissionPercent { get; set; } // % from Product.CommissionPercent
    public decimal CommissionAmount { get; set; } // Profit * CommissionPercent / 100

    public CommissionStatus Status { get; set; } = CommissionStatus.Pending;
    public DateTime? PaidDate { get; set; }
    public int? PayrollId { get; set; } // Link to payroll when paid

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CreatedByUserId { get; set; }
    public virtual ApplicationUser CreatedByUser { get; set; } = null!;
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedByUserId { get; set; }
    public virtual ApplicationUser? UpdatedByUser { get; set; }

    // Navigation properties
    public virtual PayrollResult? Payroll { get; set; }
}

/// <summary>
/// Status of a commission payment
/// </summary>
public enum CommissionStatus
{
    /// <summary>
    /// Commission calculated but not yet approved/paid
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Commission approved and scheduled for payment
    /// </summary>
    Approved = 1,

    /// <summary>
    /// Commission has been paid to the salesperson
    /// </summary>
    Paid = 2,

    /// <summary>
    /// Commission cancelled (sale was cancelled/completed, etc.)
    /// </summary>
    Cancelled = 3
}
