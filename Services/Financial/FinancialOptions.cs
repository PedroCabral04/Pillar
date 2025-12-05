namespace erp.Services.Financial;

/// <summary>
/// Configuration options for the Financial module
/// </summary>
public class FinancialOptions
{
    public const string SectionName = "Financial";

    /// <summary>
    /// Amount threshold above which accounts payable require approval
    /// </summary>
    public decimal ApprovalThresholdAmount { get; set; } = 5000m;

    /// <summary>
    /// Number of days to project cash flow into the future
    /// </summary>
    public int CashFlowProjectionDays { get; set; } = 60;

    /// <summary>
    /// Number of days of historical data to include in cash flow analysis
    /// </summary>
    public int CashFlowHistoryDays { get; set; } = 30;
}
