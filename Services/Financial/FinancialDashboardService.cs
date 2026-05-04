using erp.DAOs.Financial;
using erp.Data;
using erp.DTOs.Financial;
using erp.Models.Financial;
using erp.Models.ServiceOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace erp.Services.Financial;

public class FinancialDashboardService : IFinancialDashboardService
{
    private readonly IAccountPayableDao _payableDao;
    private readonly IAccountReceivableDao _receivableDao;
    private readonly ApplicationDbContext _dbContext;
    private readonly FinancialOptions _options;

    public FinancialDashboardService(
        IAccountPayableDao payableDao,
        IAccountReceivableDao receivableDao,
        ApplicationDbContext dbContext,
        IOptions<FinancialOptions> options)
    {
        _payableDao = payableDao;
        _receivableDao = receivableDao;
        _dbContext = dbContext;
        _options = options.Value;
    }

    public async Task<FinancialDashboardDto> GetDashboardDataAsync(DateTime? startDate = null, DateTime? endDate = null, decimal initialBalance = 0)
    {
        var allPayables = await _payableDao.GetByDateRangeAsync(startDate, endDate);
        var allReceivables = await _receivableDao.GetByDateRangeAsync(startDate, endDate);

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

        var completedStatuses = new[] { ServiceOrderStatus.Completed.ToString(), ServiceOrderStatus.Delivered.ToString() };
        var osQuery = _dbContext.ServiceOrders
            .Where(o => !o.CustomerId.HasValue && completedStatuses.Contains(o.Status));

        if (startDate.HasValue)
        {
            var filterStart = startDate.Value.ToUniversalTime();
            osQuery = osQuery.Where(o => o.ActualCompletionDate >= filterStart || (o.ActualCompletionDate == null && o.EntryDate >= filterStart));
        }
        if (endDate.HasValue)
        {
            var filterEnd = endDate.Value.ToUniversalTime();
            osQuery = osQuery.Where(o => o.ActualCompletionDate <= filterEnd || (o.ActualCompletionDate == null && o.EntryDate <= filterEnd));
        }

        var orphanServiceOrders = await osQuery.ToListAsync();
        var osRevenueTotal = orphanServiceOrders.Sum(o => o.NetAmount);

        var osByCompletionDate = orphanServiceOrders
            .Where(o => o.ActualCompletionDate.HasValue)
            .GroupBy(o => o.ActualCompletionDate!.Value.Date)
            .ToDictionary(g => g.Key, g => g.Sum(o => o.NetAmount));
        
        var dto = new FinancialDashboardDto();

        dto.TotalPayable = payablesList.Sum(x => x.NetAmount);
        dto.TotalPayableOverdue = payablesList.Where(x => x.Status == AccountStatus.Overdue).Sum(x => x.RemainingAmount);
        dto.TotalPayablePaid = payablesList.Where(x => x.Status == AccountStatus.Paid).Sum(x => x.PaidAmount);
        dto.TotalPayablePending = payablesList.Where(x => x.Status == AccountStatus.Pending).Sum(x => x.RemainingAmount);

        dto.TotalReceivable = receivablesList.Sum(x => x.NetAmount) + osRevenueTotal;
        dto.TotalReceivableOverdue = receivablesList.Where(x => x.Status == AccountStatus.Overdue).Sum(x => x.RemainingAmount);
        dto.TotalReceivablePaid = receivablesList.Where(x => x.Status == AccountStatus.Paid).Sum(x => x.PaidAmount) + osRevenueTotal;
        dto.TotalReceivablePending = receivablesList.Where(x => x.Status == AccountStatus.Pending).Sum(x => x.RemainingAmount);

        dto.PayablesCount = payablesList.Count(x => x.Status != AccountStatus.Paid && x.Status != AccountStatus.Cancelled);
        dto.PayablesOverdueCount = payablesList.Count(x => x.Status == AccountStatus.Overdue);
        dto.ReceivablesCount = receivablesList.Count(x => x.Status != AccountStatus.Paid && x.Status != AccountStatus.Cancelled);
        dto.ReceivablesOverdueCount = receivablesList.Count(x => x.Status == AccountStatus.Overdue);

        var projectionStart = startDate?.ToUniversalTime().Date ?? DateTime.UtcNow.Date;
        var projectionEnd = endDate?.ToUniversalTime().Date ?? projectionStart.AddDays(30);
        var projectionDays = (int)(projectionEnd - projectionStart).TotalDays;
        if (projectionDays <= 0) projectionDays = 30;
        if (projectionDays > 365) projectionDays = 365;
        
        var flowPayables = allPayables
            .Where(x => x.DueDate >= projectionStart && x.DueDate <= projectionEnd && x.Status != AccountStatus.Paid && x.Status != AccountStatus.Cancelled)
            .GroupBy(x => x.DueDate.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.RemainingAmount));

        var flowReceivables = allReceivables
            .Where(x => x.DueDate >= projectionStart && x.DueDate <= projectionEnd && x.Status != AccountStatus.Paid && x.Status != AccountStatus.Cancelled)
            .GroupBy(x => x.DueDate.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.RemainingAmount));

        var today = DateTime.UtcNow.Date;
        var historyDays = 30;
        var finalDate = projectionEnd;

        var flowPayablesPaid = payables
            .Where(x => x.PaymentDate.HasValue && x.PaymentDate.Value.Date >= today.AddDays(-historyDays) && x.PaymentDate.Value.Date <= finalDate && x.Status == AccountStatus.Paid)
            .GroupBy(x => x.PaymentDate!.Value.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.PaidAmount));

        var flowReceivablesPaid = receivables
            .Where(x => x.PaymentDate.HasValue && x.PaymentDate.Value.Date >= today.AddDays(-historyDays) && x.PaymentDate.Value.Date <= finalDate && x.Status == AccountStatus.Paid)
            .GroupBy(x => x.PaymentDate!.Value.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.PaidAmount));

        foreach (var kv in osByCompletionDate)
        {
            if (flowReceivablesPaid.ContainsKey(kv.Key))
                flowReceivablesPaid[kv.Key] += kv.Value;
            else
                flowReceivablesPaid[kv.Key] = kv.Value;
        }

        decimal cumulativeBalance = dto.TotalReceivablePaid - dto.TotalPayablePaid;

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

        dto.PayablesAgingList = CalculateAgingList(allPayables.Where(x => x.Status != AccountStatus.Paid && x.Status != AccountStatus.Cancelled));
        dto.ReceivablesAgingList = CalculateAgingList(allReceivables.Where(x => x.Status != AccountStatus.Paid && x.Status != AccountStatus.Cancelled));

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
        
        var d0_30 = overdue.Where(x => (today - x.DueDate).TotalDays <= 30).ToList();
        list.Add(new AgingListItemDto { Period = "0-30 dias", Count = d0_30.Count, Amount = d0_30.Sum(x => (decimal)x.RemainingAmount) });

        var d31_60 = overdue.Where(x => (today - x.DueDate).TotalDays > 30 && (today - x.DueDate).TotalDays <= 60).ToList();
        list.Add(new AgingListItemDto { Period = "31-60 dias", Count = d31_60.Count, Amount = d31_60.Sum(x => (decimal)x.RemainingAmount) });

        var d61_90 = overdue.Where(x => (today - x.DueDate).TotalDays > 60 && (today - x.DueDate).TotalDays <= 90).ToList();
        list.Add(new AgingListItemDto { Period = "61-90 dias", Count = d61_90.Count, Amount = d61_90.Sum(x => (decimal)x.RemainingAmount) });

        var d90plus = overdue.Where(x => (today - x.DueDate).TotalDays > 90).ToList();
        list.Add(new AgingListItemDto { Period = ">90 dias", Count = d90plus.Count, Amount = d90plus.Sum(x => (decimal)x.RemainingAmount) });

        return list;
    }
}
