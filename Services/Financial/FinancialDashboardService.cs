using erp.DAOs.Financial;
using erp.DTOs.Financial;
using erp.Models.Financial;
using Microsoft.Extensions.Options;

namespace erp.Services.Financial;

public class FinancialDashboardService : IFinancialDashboardService
{
    private readonly IAccountPayableDao _payableDao;
    private readonly IAccountReceivableDao _receivableDao;
    private readonly FinancialOptions _options;

    public FinancialDashboardService(
        IAccountPayableDao payableDao,
        IAccountReceivableDao receivableDao,
        IOptions<FinancialOptions> options)
    {
        _payableDao = payableDao;
        _receivableDao = receivableDao;
        _options = options.Value;
    }

    public async Task<FinancialDashboardDto> GetDashboardDataAsync(DateTime? startDate = null, DateTime? endDate = null, decimal initialBalance = 0)
    {
        var payables = await _payableDao.GetAllAsync();
        var receivables = await _receivableDao.GetAllAsync();

        // Filter by date if provided (using DueDate for general filtering if needed, but usually dashboard shows current state)
        // For specific charts like cash flow, we might filter.
        // For totals, we usually want everything relevant (e.g. pending, overdue).
        
        var dto = new FinancialDashboardDto();

        // Totals
        dto.TotalPayable = payables.Sum(x => x.NetAmount);
        dto.TotalPayableOverdue = payables.Where(x => x.Status == AccountStatus.Overdue).Sum(x => x.RemainingAmount);
        dto.TotalPayablePaid = payables.Where(x => x.Status == AccountStatus.Paid).Sum(x => x.PaidAmount);
        dto.TotalPayablePending = payables.Where(x => x.Status == AccountStatus.Pending).Sum(x => x.RemainingAmount);

        dto.TotalReceivable = receivables.Sum(x => x.NetAmount);
        dto.TotalReceivableOverdue = receivables.Where(x => x.Status == AccountStatus.Overdue).Sum(x => x.RemainingAmount);
        dto.TotalReceivablePaid = receivables.Where(x => x.Status == AccountStatus.Paid).Sum(x => x.PaidAmount);
        dto.TotalReceivablePending = receivables.Where(x => x.Status == AccountStatus.Pending).Sum(x => x.RemainingAmount);

        // Counts
        dto.PayablesCount = payables.Count(x => x.Status != AccountStatus.Paid && x.Status != AccountStatus.Cancelled);
        dto.PayablesOverdueCount = payables.Count(x => x.Status == AccountStatus.Overdue);
        dto.ReceivablesCount = receivables.Count(x => x.Status != AccountStatus.Paid && x.Status != AccountStatus.Cancelled);
        dto.ReceivablesOverdueCount = receivables.Count(x => x.Status == AccountStatus.Overdue);

        // Cash Flow Projection with cumulative balance
        // Uses configurable projection and history days
        var today = DateTime.UtcNow.Date;
        var projectionDays = _options.CashFlowProjectionDays;
        var historyDays = _options.CashFlowHistoryDays;
        var finalDate = today.AddDays(projectionDays);
        
        // Get pending accounts for future projection
        var flowPayablesPending = payables
            .Where(x => x.DueDate >= today && x.DueDate <= finalDate && x.Status != AccountStatus.Paid && x.Status != AccountStatus.Cancelled)
            .GroupBy(x => x.DueDate.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.RemainingAmount));

        var flowReceivablesPending = receivables
            .Where(x => x.DueDate >= today && x.DueDate <= finalDate && x.Status != AccountStatus.Paid && x.Status != AccountStatus.Cancelled)
            .GroupBy(x => x.DueDate.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.RemainingAmount));
        
        // Get realized (paid) accounts to show what already happened
        var flowPayablesPaid = payables
            .Where(x => x.PaymentDate.HasValue && x.PaymentDate.Value.Date >= today.AddDays(-historyDays) && x.PaymentDate.Value.Date <= finalDate && x.Status == AccountStatus.Paid)
            .GroupBy(x => x.PaymentDate!.Value.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.PaidAmount));

        var flowReceivablesPaid = receivables
            .Where(x => x.PaymentDate.HasValue && x.PaymentDate.Value.Date >= today.AddDays(-historyDays) && x.PaymentDate.Value.Date <= finalDate && x.Status == AccountStatus.Paid)
            .GroupBy(x => x.PaymentDate!.Value.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.PaidAmount));

        // Calculate cumulative balance starting from initial balance
        decimal cumulativeBalance = initialBalance;
        
        for (int i = 0; i <= projectionDays; i++)
        {
            var date = today.AddDays(i);
            
            // Pending (projected)
            var pendingRevenue = flowReceivablesPending.GetValueOrDefault(date, 0);
            var pendingExpense = flowPayablesPending.GetValueOrDefault(date, 0);
            
            // Already paid (realized) - only for today and past dates shown
            var paidRevenue = flowReceivablesPaid.GetValueOrDefault(date, 0);
            var paidExpense = flowPayablesPaid.GetValueOrDefault(date, 0);
            
            // Combine for total view
            var totalRevenue = pendingRevenue + paidRevenue;
            var totalExpense = pendingExpense + paidExpense;
            
            cumulativeBalance += (totalRevenue - totalExpense);
            
            var cashFlowItem = new CashFlowItemDto
            {
                Date = date,
                Expense = totalExpense,
                Revenue = totalRevenue,
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

        // Aging List
        dto.PayablesAgingList = CalculateAgingList(payables.Where(x => x.Status != AccountStatus.Paid && x.Status != AccountStatus.Cancelled));
        dto.ReceivablesAgingList = CalculateAgingList(receivables.Where(x => x.Status != AccountStatus.Paid && x.Status != AccountStatus.Cancelled));

        // Top Suppliers
        dto.TopSuppliers = payables
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

        // Top Customers
        dto.TopCustomers = receivables
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
