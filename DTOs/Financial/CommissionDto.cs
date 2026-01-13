using erp.Models.Financial;

namespace erp.DTOs.Financial;

/// <summary>
/// DTO for commission display
/// </summary>
public class CommissionDto
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public int SaleItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public decimal ProfitAmount { get; set; }
    public decimal CommissionPercent { get; set; }
    public decimal CommissionAmount { get; set; }
    public CommissionStatus Status { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
    public DateTime? PaidDate { get; set; }
    public int? PayrollId { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for commission summary by user/month
/// </summary>
public class CommissionSummaryDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int TotalSales { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal PaidCommission { get; set; }
    public decimal PendingCommission { get; set; }
    public List<CommissionDto> Commissions { get; set; } = new();
}

/// <summary>
/// DTO for commission report by user
/// </summary>
public class CommissionReportDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public List<MonthlyCommissionDto> MonthlyCommissions { get; set; } = new();
    public decimal TotalCommission { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalPending { get; set; }
}

/// <summary>
/// Monthly commission breakdown
/// </summary>
public class MonthlyCommissionDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int SaleCount { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }
}
