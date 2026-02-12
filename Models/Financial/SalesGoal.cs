using System.ComponentModel.DataAnnotations;
using erp.Models.Audit;
using erp.Models.Identity;
using Microsoft.EntityFrameworkCore;

namespace erp.Models.Financial;

/// <summary>
/// Sales goals for vendors/sellers by month
/// </summary>
public class SalesGoal : IAuditable, IMustHaveTenant
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    /// <summary>
    /// User (vendor/seller) this goal is for
    /// </summary>
    public int UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Year of the goal (e.g., 2026)
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Month of the goal (1-12)
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Target sales amount in currency
    /// </summary>
    [Precision(18, 2)]
    public decimal TargetSalesAmount { get; set; }

    /// <summary>
    /// Target profit amount in currency
    /// </summary>
    [Precision(18, 2)]
    public decimal TargetProfitAmount { get; set; }

    /// <summary>
    /// Target number of sales transactions
    /// </summary>
    public int TargetSalesCount { get; set; }

    /// <summary>
    /// Bonus commission percent if goal is achieved (applied on top of regular commissions)
    /// </summary>
    [Precision(5, 2)]
    public decimal BonusCommissionPercent { get; set; }

    /// <summary>
    /// Notes about the goal
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public int CreatedByUserId { get; set; }
    public virtual ApplicationUser CreatedByUser { get; set; } = null!;
    public int? UpdatedByUserId { get; set; }
    public virtual ApplicationUser? UpdatedByUser { get; set; }
}
