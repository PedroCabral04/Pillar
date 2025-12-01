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

        // Cash Flow Projection (Next 30 days) with cumulative balance
        var today = DateTime.UtcNow.Date;
        var next30Days = today.AddDays(30);
        
        var flowPayables = payables
            .Where(x => x.DueDate >= today && x.DueDate <= next30Days && x.Status != AccountStatus.Paid && x.Status != AccountStatus.Cancelled)
            .GroupBy(x => x.DueDate.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.RemainingAmount));

        var flowReceivables = receivables
            .Where(x => x.DueDate >= today && x.DueDate <= next30Days && x.Status != AccountStatus.Paid && x.Status != AccountStatus.Cancelled)
            .GroupBy(x => x.DueDate.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.RemainingAmount));

        // Calculate cumulative balance starting from initial balance
        decimal cumulativeBalance = initialBalance;
        
        for (int i = 0; i <= 30; i++)
        {
            var date = today.AddDays(i);
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
                    Message = $"Saldo projetado negativo de {cumulativeBalance:C2} em {date:dd/MM/yyyy}"
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
