using erp.DAOs.Financial;
using erp.DTOs.Financial;
using erp.Models.Financial;

namespace erp.Services.Financial;

public class FinancialDashboardService : IFinancialDashboardService
{
    private readonly IAccountPayableDao _payableDao;
    private readonly IAccountReceivableDao _receivableDao;

    public FinancialDashboardService(
        IAccountPayableDao payableDao,
        IAccountReceivableDao receivableDao)
    {
        _payableDao = payableDao;
        _receivableDao = receivableDao;
    }

    public async Task<FinancialDashboardDto> GetDashboardDataAsync(DateTime? startDate = null, DateTime? endDate = null, decimal initialBalance = 0)
    {
        var allPayables = await _payableDao.GetAllAsync();
        var allReceivables = await _receivableDao.GetAllAsync();
        
        // Apply date filter if provided (using IssueDate for historical analysis)
        var payables = allPayables.AsEnumerable();
        var receivables = allReceivables.AsEnumerable();
        
        if (startDate.HasValue)
        {
            var filterStart = startDate.Value.ToUniversalTime().Date;
            payables = payables.Where(x => x.IssueDate >= filterStart);
            receivables = receivables.Where(x => x.IssueDate >= filterStart);
        }
        
        if (endDate.HasValue)
        {
            var filterEnd = endDate.Value.ToUniversalTime().Date.AddDays(1);
            payables = payables.Where(x => x.IssueDate < filterEnd);
            receivables = receivables.Where(x => x.IssueDate < filterEnd);
        }
        
        var payablesList = payables.ToList();
        var receivablesList = receivables.ToList();
        
        var dto = new FinancialDashboardDto();

        // Totals (filtered by period)
        dto.TotalPayable = payablesList.Sum(x => x.NetAmount);
        dto.TotalPayableOverdue = payablesList.Where(x => x.Status == AccountStatus.Overdue).Sum(x => x.RemainingAmount);
        dto.TotalPayablePaid = payablesList.Where(x => x.Status == AccountStatus.Paid).Sum(x => x.PaidAmount);
        dto.TotalPayablePending = payablesList.Where(x => x.Status == AccountStatus.Pending).Sum(x => x.RemainingAmount);

        dto.TotalReceivable = receivablesList.Sum(x => x.NetAmount);
        dto.TotalReceivableOverdue = receivablesList.Where(x => x.Status == AccountStatus.Overdue).Sum(x => x.RemainingAmount);
        dto.TotalReceivablePaid = receivablesList.Where(x => x.Status == AccountStatus.Paid).Sum(x => x.PaidAmount);
        dto.TotalReceivablePending = receivablesList.Where(x => x.Status == AccountStatus.Pending).Sum(x => x.RemainingAmount);

        // Counts
        dto.PayablesCount = payablesList.Count(x => x.Status != AccountStatus.Paid && x.Status != AccountStatus.Cancelled);
        dto.PayablesOverdueCount = payablesList.Count(x => x.Status == AccountStatus.Overdue);
        dto.ReceivablesCount = receivablesList.Count(x => x.Status != AccountStatus.Paid && x.Status != AccountStatus.Cancelled);
        dto.ReceivablesOverdueCount = receivablesList.Count(x => x.Status == AccountStatus.Overdue);

        // Cash Flow Projection - Use date range if provided, otherwise next 30 days
        var projectionStart = startDate?.ToUniversalTime().Date ?? DateTime.UtcNow.Date;
        var projectionEnd = endDate?.ToUniversalTime().Date ?? projectionStart.AddDays(30);
        var projectionDays = (int)(projectionEnd - projectionStart).TotalDays;
        if (projectionDays <= 0) projectionDays = 30;
        if (projectionDays > 365) projectionDays = 365; // Cap at 1 year for performance
        
        // For projection, we need all unpaid accounts with DueDate in the range
        var flowPayables = allPayables
            .Where(x => x.DueDate >= projectionStart && x.DueDate <= projectionEnd && x.Status != AccountStatus.Paid && x.Status != AccountStatus.Cancelled)
            .GroupBy(x => x.DueDate.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.RemainingAmount));

        var flowReceivables = allReceivables
            .Where(x => x.DueDate >= projectionStart && x.DueDate <= projectionEnd && x.Status != AccountStatus.Paid && x.Status != AccountStatus.Cancelled)
            .GroupBy(x => x.DueDate.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.RemainingAmount));

        // Calculate cumulative balance starting from initial balance
        decimal cumulativeBalance = initialBalance;
        
        for (int i = 0; i <= projectionDays; i++)
        {
            var date = projectionStart.AddDays(i);
            var revenue = flowReceivables.ContainsKey(date) ? flowReceivables[date] : 0;
            var expense = flowPayables.ContainsKey(date) ? flowPayables[date] : 0;
            
            cumulativeBalance += (revenue - expense);
            
            var cashFlowItem = new CashFlowItemDto
            {
                Date = date,
                Expense = expense,
                Revenue = revenue,
                CumulativeBalance = cumulativeBalance
            };
            
            dto.CashFlowProjection.Add(cashFlowItem);
            
            // Add alert for negative balance days
            if (cumulativeBalance < 0)
            {
                dto.CashFlowAlerts.Add(new CashFlowAlertDto
                {
                    Date = date,
                    ProjectedBalance = cumulativeBalance,
                    Shortfall = Math.Abs(cumulativeBalance),
                    Severity = cumulativeBalance < -10000 ? "Critical" : "Warning",
                    Message = $"Saldo projetado negativo de {CurrencyFormatService.FormatStatic(cumulativeBalance)} em {date:dd/MM/yyyy}"
                });
            }
        }

        // Aging List (uses all accounts, not date-filtered, as aging is about current overdue status)
        dto.PayablesAgingList = CalculateAgingList(allPayables.Where(x => x.Status != AccountStatus.Paid && x.Status != AccountStatus.Cancelled));
        dto.ReceivablesAgingList = CalculateAgingList(allReceivables.Where(x => x.Status != AccountStatus.Paid && x.Status != AccountStatus.Cancelled));

        // Top Suppliers (filtered by period)
        dto.TopSuppliers = payablesList
            .Where(x => x.Supplier != null)
            .GroupBy(x => x.SupplierId)
            .Select(g => new TopCustomerSupplierDto
            {
                Id = g.Key,
                Name = g.First().Supplier!.Name,
                TotalAmount = g.Sum(x => x.OriginalAmount),
                TransactionCount = g.Count()
            })
            .OrderByDescending(x => x.TotalAmount)
            .Take(5)
            .ToList();

        // Top Customers (filtered by period)
        dto.TopCustomers = receivablesList
            .Where(x => x.Customer != null)
            .GroupBy(x => x.CustomerId)
            .Select(g => new TopCustomerSupplierDto
            {
                Id = g.Key,
                Name = g.First().Customer!.Name,
                TotalAmount = g.Sum(x => x.OriginalAmount),
                TransactionCount = g.Count()
            })
            .OrderByDescending(x => x.TotalAmount)
            .Take(5)
            .ToList();

        return dto;
    }

    private List<AgingListItemDto> CalculateAgingList(IEnumerable<dynamic> accounts)
    {
        var today = DateTime.UtcNow.Date;
        var list = new List<AgingListItemDto>();

        var overdue = accounts.Where(x => x.DueDate < today).ToList();
        
        // 0-30 days overdue
        var d0_30 = overdue.Where(x => (today - x.DueDate).TotalDays <= 30).ToList();
        list.Add(new AgingListItemDto { Period = "0-30 dias", Count = d0_30.Count, Amount = d0_30.Sum(x => (decimal)x.RemainingAmount) });

        // 31-60 days overdue
        var d31_60 = overdue.Where(x => (today - x.DueDate).TotalDays > 30 && (today - x.DueDate).TotalDays <= 60).ToList();
        list.Add(new AgingListItemDto { Period = "31-60 dias", Count = d31_60.Count, Amount = d31_60.Sum(x => (decimal)x.RemainingAmount) });

        // 61-90 days overdue
        var d61_90 = overdue.Where(x => (today - x.DueDate).TotalDays > 60 && (today - x.DueDate).TotalDays <= 90).ToList();
        list.Add(new AgingListItemDto { Period = "61-90 dias", Count = d61_90.Count, Amount = d61_90.Sum(x => (decimal)x.RemainingAmount) });

        // >90 days overdue
        var d90plus = overdue.Where(x => (today - x.DueDate).TotalDays > 90).ToList();
        list.Add(new AgingListItemDto { Period = ">90 dias", Count = d90plus.Count, Amount = d90plus.Sum(x => (decimal)x.RemainingAmount) });

        return list;
    }
}
