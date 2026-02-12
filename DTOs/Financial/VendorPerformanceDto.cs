namespace erp.DTOs.Financial;

/// <summary>
/// DTO para exibição de performance de vendedores
/// </summary>
public class VendorPerformanceDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int TotalSalesCount { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public decimal TotalProfitAmount { get; set; }
    public decimal TotalCommissionEarned { get; set; }
    public decimal TotalCommissionPaid { get; set; }
    public decimal TotalCommissionPending { get; set; }
    public decimal BonusCommissionEarned { get; set; }
    public decimal? SalesGoalTarget { get; set; }
    public decimal? SalesGoalAchievementPercent { get; set; }
    public bool SalesGoalAchieved { get; set; }
    public DateTime LastCalculatedAt { get; set; }
}

/// <summary>
/// DTO para lista de performance de vendedores
/// </summary>
public class VendorPerformanceListDto
{
    public List<VendorPerformanceDto> Items { get; set; } = new();
    public int Total { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
