namespace erp.DTOs.Reports;

/// <summary>
/// DTO for financial report filters
/// </summary>
public class FinancialReportFilterDto : ReportFilterDto
{
    public string ReportType { get; set; } = "cash-flow"; // cash-flow, profit-loss, balance-sheet
    public int? CategoryId { get; set; }
    public int? CostCenterId { get; set; }
    public int? SupplierId { get; set; }
}

/// <summary>
/// DTO for cash flow report
/// </summary>
public class CashFlowReportDto
{
    public List<CashFlowItemDto> Items { get; set; } = new();
    public CashFlowSummaryDto Summary { get; set; } = new();
}

public class CashFlowItemDto
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Receita/Despesa
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CashFlowSummaryDto
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetCashFlow { get; set; }
    public decimal PendingReceivables { get; set; }
    public decimal PendingPayables { get; set; }
}

/// <summary>
/// DTO for profit and loss statement
/// </summary>
public class ProfitLossReportDto
{
    public decimal TotalRevenue { get; set; }
    public decimal CostOfGoodsSold { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal OperatingExpenses { get; set; }
    public decimal OperatingIncome { get; set; }
    public decimal OtherIncome { get; set; }
    public decimal OtherExpenses { get; set; }
    public decimal NetIncome { get; set; }
    public decimal GrossProfitMargin { get; set; }
    public decimal NetProfitMargin { get; set; }
    public List<ExpenseByCategory> ExpensesByCategory { get; set; } = new();
}

public class ExpenseByCategory
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// DTO for balance sheet
/// </summary>
public class BalanceSheetReportDto
{
    public decimal TotalAssets { get; set; }
    public decimal CurrentAssets { get; set; }
    public decimal FixedAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal CurrentLiabilities { get; set; }
    public decimal LongTermLiabilities { get; set; }
    public decimal Equity { get; set; }
    public decimal RetainedEarnings { get; set; }
}
