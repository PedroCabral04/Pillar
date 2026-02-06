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
    public Models.Financial.PaymentMethod? PaymentMethod { get; set; }
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
    public string PaymentMethod { get; set; } = string.Empty;
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
    public Dictionary<string, decimal> RevenueByPaymentMethod { get; set; } = new();
    public Dictionary<string, decimal> ExpensesByPaymentMethod { get; set; } = new();
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

/// <summary>
/// DTO for daily cash closing report (fechamento de caixa diario)
/// </summary>
public class DailyClosingReportDto
{
    public DailyClosingSummaryDto Summary { get; set; } = new();

    public List<DailyClosingItemDto> RealizedEntries { get; set; } = new();
    public List<DailyClosingItemDto> RealizedExits { get; set; } = new();
    public List<DailyClosingItemDto> DueEntries { get; set; } = new();
    public List<DailyClosingItemDto> DueExits { get; set; } = new();

    public List<DailyClosingItemDto> OverdueReceivables { get; set; } = new();
    public List<DailyClosingItemDto> OverduePayables { get; set; } = new();

    public Dictionary<string, decimal> RealizedEntriesByPaymentMethod { get; set; } = new();
    public Dictionary<string, decimal> RealizedExitsByPaymentMethod { get; set; } = new();
    public Dictionary<string, decimal> DueEntriesByPaymentMethod { get; set; } = new();
    public Dictionary<string, decimal> DueExitsByPaymentMethod { get; set; } = new();

    public List<DailyClosingGroupDto> RealizedByCategory { get; set; } = new();
    public List<DailyClosingGroupDto> DueByCategory { get; set; } = new();
    public List<DailyClosingGroupDto> RealizedByCostCenter { get; set; } = new();
    public List<DailyClosingGroupDto> DueByCostCenter { get; set; } = new();

    public List<DailyClosingItemDto> TopRealizedMovements { get; set; } = new();
    public List<DailyClosingItemDto> TopDueMovements { get; set; } = new();
}

public class DailyClosingSummaryDto
{
    public DateTime ReportDate { get; set; }

    public decimal OpeningBalance { get; set; }
    public decimal TotalEntriesRealized { get; set; }
    public decimal TotalExitsRealized { get; set; }
    public decimal NetRealized { get; set; }
    public decimal ClosingBalance { get; set; }

    public decimal TotalEntriesDue { get; set; }
    public decimal TotalExitsDue { get; set; }
    public decimal NetDue { get; set; }

    public decimal DifferenceRealizedVsDue { get; set; }

    public int RealizedEntriesCount { get; set; }
    public int RealizedExitsCount { get; set; }
    public int DueEntriesCount { get; set; }
    public int DueExitsCount { get; set; }

    public int OverdueReceivablesCount { get; set; }
    public int OverduePayablesCount { get; set; }
    public decimal OverdueReceivablesAmount { get; set; }
    public decimal OverduePayablesAmount { get; set; }
}

public class DailyClosingItemDto
{
    public int Id { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Entrada/Saida
    public string Counterparty { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string CostCenter { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaymentDate { get; set; }

    public decimal OriginalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal FineAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }

    public bool IsOverdue { get; set; }
}

public class DailyClosingGroupDto
{
    public string Group { get; set; } = string.Empty;
    public decimal Entries { get; set; }
    public decimal Exits { get; set; }
    public decimal Net => Entries - Exits;
    public int Count { get; set; }
}
