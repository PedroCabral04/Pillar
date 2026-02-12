using erp.Models.Audit;
using erp.Models.Identity;
using Microsoft.EntityFrameworkCore;

namespace erp.Models.Financial;

/// <summary>
/// Monthly performance summary for vendors/sellers (denormalized for reporting)
/// </summary>
public class VendorPerformance : IAuditable, IMustHaveTenant
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    /// <summary>
    /// User (vendor/seller) this performance is for
    /// </summary>
    public int UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Year of the performance period
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Month of the performance period (1-12)
    /// </summary>
    public int Month { get; set; }

    // Sales Metrics
    public int TotalSalesCount { get; set; }
    [Precision(18, 2)]
    public decimal TotalSalesAmount { get; set; }
    [Precision(18, 2)]
    public decimal TotalProfitAmount { get; set; }

    // Commission Metrics
    [Precision(18, 2)]
    public decimal TotalCommissionEarned { get; set; }
    [Precision(18, 2)]
    public decimal TotalCommissionPaid { get; set; }
    [Precision(18, 2)]
    public decimal TotalCommissionPending { get; set; }
    [Precision(18, 2)]
    public decimal BonusCommissionEarned { get; set; }

    // Goal Achievement
    [Precision(18, 2)]
    public decimal? SalesGoalTarget { get; set; }
    [Precision(5, 2)]
    public decimal? SalesGoalAchievementPercent { get; set; }
    public bool SalesGoalAchieved { get; set; }

    /// <summary>
    /// When this summary was last calculated
    /// </summary>
    public DateTime LastCalculatedAt { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
