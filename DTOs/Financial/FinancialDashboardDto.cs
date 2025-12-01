namespace erp.DTOs.Financial;

/// <summary>
/// DTO for financial dashboard overview
/// </summary>
public class FinancialDashboardDto
{
    public decimal TotalReceivable { get; set; }
    public decimal TotalReceivableOverdue { get; set; }
    public decimal TotalReceivablePaid { get; set; }
    public decimal TotalReceivablePending { get; set; }
    
    public decimal TotalPayable { get; set; }
    public decimal TotalPayableOverdue { get; set; }
    public decimal TotalPayablePaid { get; set; }
    public decimal TotalPayablePending { get; set; }
    
    public decimal CashBalance => TotalReceivablePaid - TotalPayablePaid;
    public decimal ForecastedBalance => TotalReceivable - TotalPayable;
    
    public int ReceivablesCount { get; set; }
    public int ReceivablesOverdueCount { get; set; }
    public int PayablesCount { get; set; }
    public int PayablesOverdueCount { get; set; }
    
    public List<CashFlowItemDto> CashFlowProjection { get; set; } = new();
    public List<CashFlowAlertDto> CashFlowAlerts { get; set; } = new();
    public List<AgingListItemDto> ReceivablesAgingList { get; set; } = new();
    public List<AgingListItemDto> PayablesAgingList { get; set; } = new();
    public List<TopCustomerSupplierDto> TopCustomers { get; set; } = new();
    public List<TopCustomerSupplierDto> TopSuppliers { get; set; } = new();
}

/// <summary>
/// DTO for cash flow item
/// </summary>
public class CashFlowItemDto
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public decimal Expense { get; set; }
    public decimal Balance => Revenue - Expense;
    public decimal CumulativeBalance { get; set; }
    public bool IsNegative => CumulativeBalance < 0;
}

/// <summary>
/// DTO for cash flow alert (negative balance days)
/// </summary>
public class CashFlowAlertDto
{
    public DateTime Date { get; set; }
    public decimal ProjectedBalance { get; set; }
    public decimal Shortfall { get; set; }
    public string Severity { get; set; } = "Warning"; // Warning, Critical
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// DTO for aging list item
/// </summary>
public class AgingListItemDto
{
    public string Period { get; set; } = string.Empty; // "0-30", "31-60", "61-90", ">90"
    public int Count { get; set; }
    public decimal Amount { get; set; }
}

/// <summary>
/// DTO for top customer/supplier
/// </summary>
public class TopCustomerSupplierDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
}

/// <summary>
/// DTO for financial summary by category
/// </summary>
public class FinancialSummaryByCategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// DTO for financial summary by cost center
/// </summary>
public class FinancialSummaryByCostCenterDto
{
    public int CostCenterId { get; set; }
    public string CostCenterName { get; set; } = string.Empty;
    public string CostCenterCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal Budget { get; set; }
    public decimal BudgetUsagePercentage => Budget > 0 ? (TotalAmount / Budget) * 100 : 0;
    public int TransactionCount { get; set; }
}

/// <summary>
/// DTO for cost center budget usage summary
/// </summary>
public class CostCenterSummaryDto
{
    public int CostCenterId { get; set; }
    public string CostCenterName { get; set; } = string.Empty;
    public decimal MonthlyBudget { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal Remaining { get; set; }
    public decimal PercentUsed { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}
