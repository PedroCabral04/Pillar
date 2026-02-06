using erp.Data;
using erp.DTOs.Reports;
using erp.Extensions;
using erp.Models.Financial;
using Microsoft.EntityFrameworkCore;

namespace erp.Services.Reports;

public class FinancialReportService : IFinancialReportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FinancialReportService> _logger;

    public FinancialReportService(ApplicationDbContext context, ILogger<FinancialReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CashFlowReportDto> GenerateCashFlowReportAsync(FinancialReportFilterDto filter)
    {
        try
        {
            var receivablesQuery = _context.AccountsReceivable
                .Include(a => a.Category)
                .Include(a => a.Customer)
                .AsQueryable();

            var payablesQuery = _context.AccountsPayable
                .Include(a => a.Category)
                .Include(a => a.Supplier)
                .AsQueryable();

            if (filter.CategoryId.HasValue)
            {
                receivablesQuery = receivablesQuery.Where(a => a.CategoryId == filter.CategoryId.Value);
                payablesQuery = payablesQuery.Where(a => a.CategoryId == filter.CategoryId.Value);
            }

            if (filter.CostCenterId.HasValue)
            {
                receivablesQuery = receivablesQuery.Where(a => a.CostCenterId == filter.CostCenterId.Value);
                payablesQuery = payablesQuery.Where(a => a.CostCenterId == filter.CostCenterId.Value);
            }

            if (filter.SupplierId.HasValue)
            {
                payablesQuery = payablesQuery.Where(a => a.SupplierId == filter.SupplierId.Value);
            }

            if (filter.PaymentMethod.HasValue)
            {
                receivablesQuery = receivablesQuery.Where(a => a.PaymentMethod == filter.PaymentMethod.Value);
                payablesQuery = payablesQuery.Where(a => a.PaymentMethod == filter.PaymentMethod.Value);
            }

            // Apply date filters
            if (filter.StartDate.HasValue)
            {
                var startDate = filter.StartDate.Value.ToUniversalTime();
                receivablesQuery = receivablesQuery.Where(a => a.DueDate >= startDate);
                payablesQuery = payablesQuery.Where(a => a.DueDate >= startDate);
            }

            if (filter.EndDate.HasValue)
            {
                var endDate = filter.EndDate.Value.ToUniversalTime();
                receivablesQuery = receivablesQuery.Where(a => a.DueDate <= endDate);
                payablesQuery = payablesQuery.Where(a => a.DueDate <= endDate);
            }

            var receivables = await receivablesQuery.ToListAsync();
            var payables = await payablesQuery.ToListAsync();

            var items = new List<CashFlowItemDto>();

            // Add receivables
            items.AddRange(receivables.Select(r => new CashFlowItemDto
            {
                Date = r.DueDate,
                Description = r.Customer?.Name ?? "Conta a receber",
                Type = "Receita",
                Category = r.Category?.Name ?? "Sem categoria",
                PaymentMethod = r.PaymentMethod.ToDisplayName(),
                Amount = r.NetAmount,
                Status = r.Status.ToString()
            }));

            // Add payables
            items.AddRange(payables.Select(p => new CashFlowItemDto
            {
                Date = p.DueDate,
                Description = p.Supplier?.Name ?? "Conta a pagar",
                Type = "Despesa",
                Category = p.Category?.Name ?? "Sem categoria",
                PaymentMethod = p.PaymentMethod.ToDisplayName(),
                Amount = p.NetAmount,
                Status = p.Status.ToString()
            }));

            items = items.OrderBy(i => i.Date).ToList();

            var summary = new CashFlowSummaryDto
            {
                TotalRevenue = receivables.Where(r => r.Status == AccountStatus.Paid).Sum(r => r.NetAmount),
                TotalExpenses = payables.Where(p => p.Status == AccountStatus.Paid).Sum(p => p.NetAmount),
                PendingReceivables = receivables.Where(r => r.Status == AccountStatus.Pending).Sum(r => r.NetAmount),
                PendingPayables = payables.Where(p => p.Status == AccountStatus.Pending).Sum(p => p.NetAmount),
                RevenueByPaymentMethod = receivables
                    .Where(r => r.Status == AccountStatus.Paid)
                    .GroupBy(r => r.PaymentMethod.ToDisplayName())
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.NetAmount)),
                ExpensesByPaymentMethod = payables
                    .Where(p => p.Status == AccountStatus.Paid)
                    .GroupBy(p => p.PaymentMethod.ToDisplayName())
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.NetAmount))
            };

            summary.NetCashFlow = summary.TotalRevenue - summary.TotalExpenses;

            return new CashFlowReportDto
            {
                Items = items,
                Summary = summary
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório de fluxo de caixa");
            throw;
        }
    }

    public async Task<DailyClosingReportDto> GenerateDailyClosingReportAsync(FinancialReportFilterDto filter)
    {
        try
        {
            var reportDate = (filter.StartDate ?? DateTime.UtcNow).ToUniversalTime().Date;
            var nextDate = reportDate.AddDays(1);

            var receivablesQuery = _context.AccountsReceivable
                .Include(a => a.Category)
                .Include(a => a.CostCenter)
                .Include(a => a.Customer)
                .AsQueryable();

            var payablesQuery = _context.AccountsPayable
                .Include(a => a.Category)
                .Include(a => a.CostCenter)
                .Include(a => a.Supplier)
                .AsQueryable();

            if (filter.CategoryId.HasValue)
            {
                receivablesQuery = receivablesQuery.Where(a => a.CategoryId == filter.CategoryId.Value);
                payablesQuery = payablesQuery.Where(a => a.CategoryId == filter.CategoryId.Value);
            }

            if (filter.CostCenterId.HasValue)
            {
                receivablesQuery = receivablesQuery.Where(a => a.CostCenterId == filter.CostCenterId.Value);
                payablesQuery = payablesQuery.Where(a => a.CostCenterId == filter.CostCenterId.Value);
            }

            if (filter.SupplierId.HasValue)
            {
                payablesQuery = payablesQuery.Where(a => a.SupplierId == filter.SupplierId.Value);
            }

            if (filter.PaymentMethod.HasValue)
            {
                receivablesQuery = receivablesQuery.Where(a => a.PaymentMethod == filter.PaymentMethod.Value);
                payablesQuery = payablesQuery.Where(a => a.PaymentMethod == filter.PaymentMethod.Value);
            }

            var receivables = await receivablesQuery.ToListAsync();
            var payables = await payablesQuery.ToListAsync();

            var openingEntries = receivables
                .Where(r => r.PaymentDate.HasValue
                    && r.PaymentDate.Value.Date < reportDate
                    && (r.Status == AccountStatus.Paid || r.Status == AccountStatus.PartiallyPaid))
                .Sum(r => r.PaidAmount);

            var openingExits = payables
                .Where(p => p.PaymentDate.HasValue
                    && p.PaymentDate.Value.Date < reportDate
                    && (p.Status == AccountStatus.Paid || p.Status == AccountStatus.PartiallyPaid))
                .Sum(p => p.PaidAmount);

            var realizedEntries = receivables
                .Where(r => r.PaymentDate.HasValue
                    && r.PaymentDate.Value.Date >= reportDate
                    && r.PaymentDate.Value.Date < nextDate
                    && (r.Status == AccountStatus.Paid || r.Status == AccountStatus.PartiallyPaid)
                    && r.PaidAmount > 0)
                .Select(r => MapReceivableToDailyClosingItem(r, reportDate))
                .OrderBy(x => x.PaymentDate)
                .ThenBy(x => x.DueDate)
                .ToList();

            var realizedExits = payables
                .Where(p => p.PaymentDate.HasValue
                    && p.PaymentDate.Value.Date >= reportDate
                    && p.PaymentDate.Value.Date < nextDate
                    && (p.Status == AccountStatus.Paid || p.Status == AccountStatus.PartiallyPaid)
                    && p.PaidAmount > 0)
                .Select(p => MapPayableToDailyClosingItem(p, reportDate))
                .OrderBy(x => x.PaymentDate)
                .ThenBy(x => x.DueDate)
                .ToList();

            var dueEntries = receivables
                .Where(r => r.DueDate.Date >= reportDate
                    && r.DueDate.Date < nextDate
                    && r.Status != AccountStatus.Cancelled)
                .Select(r => MapReceivableToDailyClosingItem(r, reportDate))
                .OrderBy(x => x.DueDate)
                .ToList();

            var dueExits = payables
                .Where(p => p.DueDate.Date >= reportDate
                    && p.DueDate.Date < nextDate
                    && p.Status != AccountStatus.Cancelled)
                .Select(p => MapPayableToDailyClosingItem(p, reportDate))
                .OrderBy(x => x.DueDate)
                .ToList();

            var overdueReceivables = receivables
                .Where(r => r.DueDate.Date < reportDate
                    && r.Status != AccountStatus.Paid
                    && r.Status != AccountStatus.Cancelled
                    && r.RemainingAmount > 0)
                .Select(r => MapReceivableToDailyClosingItem(r, reportDate))
                .OrderByDescending(x => x.DueDate)
                .ToList();

            var overduePayables = payables
                .Where(p => p.DueDate.Date < reportDate
                    && p.Status != AccountStatus.Paid
                    && p.Status != AccountStatus.Cancelled
                    && p.RemainingAmount > 0)
                .Select(p => MapPayableToDailyClosingItem(p, reportDate))
                .OrderByDescending(x => x.DueDate)
                .ToList();

            var openingBalance = openingEntries - openingExits;
            var totalEntriesRealized = realizedEntries.Sum(GetMovementAmount);
            var totalExitsRealized = realizedExits.Sum(GetMovementAmount);
            var netRealized = totalEntriesRealized - totalExitsRealized;
            var closingBalance = openingBalance + netRealized;

            var totalEntriesDue = dueEntries.Sum(GetMovementAmount);
            var totalExitsDue = dueExits.Sum(GetMovementAmount);
            var netDue = totalEntriesDue - totalExitsDue;

            var summary = new DailyClosingSummaryDto
            {
                ReportDate = reportDate,
                OpeningBalance = openingBalance,
                TotalEntriesRealized = totalEntriesRealized,
                TotalExitsRealized = totalExitsRealized,
                NetRealized = netRealized,
                ClosingBalance = closingBalance,
                TotalEntriesDue = totalEntriesDue,
                TotalExitsDue = totalExitsDue,
                NetDue = netDue,
                DifferenceRealizedVsDue = netRealized - netDue,
                RealizedEntriesCount = realizedEntries.Count,
                RealizedExitsCount = realizedExits.Count,
                DueEntriesCount = dueEntries.Count,
                DueExitsCount = dueExits.Count,
                OverdueReceivablesCount = overdueReceivables.Count,
                OverduePayablesCount = overduePayables.Count,
                OverdueReceivablesAmount = overdueReceivables.Sum(x => x.RemainingAmount),
                OverduePayablesAmount = overduePayables.Sum(x => x.RemainingAmount)
            };

            return new DailyClosingReportDto
            {
                Summary = summary,
                RealizedEntries = realizedEntries,
                RealizedExits = realizedExits,
                DueEntries = dueEntries,
                DueExits = dueExits,
                OverdueReceivables = overdueReceivables,
                OverduePayables = overduePayables,
                RealizedEntriesByPaymentMethod = GroupByPaymentMethod(realizedEntries),
                RealizedExitsByPaymentMethod = GroupByPaymentMethod(realizedExits),
                DueEntriesByPaymentMethod = GroupByPaymentMethod(dueEntries),
                DueExitsByPaymentMethod = GroupByPaymentMethod(dueExits),
                RealizedByCategory = GroupByDimension(realizedEntries, realizedExits, x => x.Category),
                DueByCategory = GroupByDimension(dueEntries, dueExits, x => x.Category),
                RealizedByCostCenter = GroupByDimension(realizedEntries, realizedExits, x => x.CostCenter),
                DueByCostCenter = GroupByDimension(dueEntries, dueExits, x => x.CostCenter),
                TopRealizedMovements = realizedEntries
                    .Concat(realizedExits)
                    .OrderByDescending(GetMovementAmount)
                    .Take(10)
                    .ToList(),
                TopDueMovements = dueEntries
                    .Concat(dueExits)
                    .OrderByDescending(GetMovementAmount)
                    .Take(10)
                    .ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório de fechamento diário de caixa");
            throw;
        }
    }

    public async Task<ProfitLossReportDto> GenerateProfitLossReportAsync(FinancialReportFilterDto filter)
    {
        try
        {
            // Get sales revenue
            var salesQuery = _context.Sales.AsQueryable();
            var serviceOrdersQuery = _context.ServiceOrders.AsQueryable();
            
            if (filter.StartDate.HasValue)
            {
                var startDate = filter.StartDate.Value.ToUniversalTime();
                salesQuery = salesQuery.Where(s => s.SaleDate >= startDate);
                serviceOrdersQuery = serviceOrdersQuery.Where(o => o.EntryDate >= startDate);
            }
            
            if (filter.EndDate.HasValue)
            {
                var endDate = filter.EndDate.Value.ToUniversalTime();
                salesQuery = salesQuery.Where(s => s.SaleDate <= endDate);
                serviceOrdersQuery = serviceOrdersQuery.Where(o => o.EntryDate <= endDate);
            }

            var sales = await salesQuery
                .Where(s => s.Status == "Finalizada")
                .ToListAsync();

            var serviceOrders = await serviceOrdersQuery
                .Where(o => o.Status == "Completed" || o.Status == "Delivered")
                .ToListAsync();

            var totalRevenue = sales.Sum(s => s.NetAmount) + serviceOrders.Sum(o => o.NetAmount);

            // Get COGS (Cost of Goods Sold) from stock movements
            var movementsQuery = _context.StockMovements
                .Include(m => m.Product)
                .Where(m => m.Type == Models.Inventory.MovementType.Out);

            if (filter.StartDate.HasValue)
            {
                var startDate = filter.StartDate.Value.ToUniversalTime();
                movementsQuery = movementsQuery.Where(m => m.MovementDate >= startDate);
            }
            
            if (filter.EndDate.HasValue)
            {
                var endDate = filter.EndDate.Value.ToUniversalTime();
                movementsQuery = movementsQuery.Where(m => m.MovementDate <= endDate);
            }

            var movements = await movementsQuery.ToListAsync();
            var costOfGoodsSold = movements.Sum(m => m.Quantity * m.UnitCost);

            // Get operating expenses (accounts payable)
            var expensesQuery = _context.AccountsPayable
                .Include(a => a.Category)
                .Where(a => a.Status == AccountStatus.Paid);

            if (filter.StartDate.HasValue)
            {
                var startDate = filter.StartDate.Value.ToUniversalTime();
                expensesQuery = expensesQuery.Where(a => a.PaymentDate >= startDate);
            }
            
            if (filter.EndDate.HasValue)
            {
                var endDate = filter.EndDate.Value.ToUniversalTime();
                expensesQuery = expensesQuery.Where(a => a.PaymentDate <= endDate);
            }

            if (filter.CategoryId.HasValue)
            {
                expensesQuery = expensesQuery.Where(a => a.CategoryId == filter.CategoryId.Value);
            }

            if (filter.CostCenterId.HasValue)
            {
                expensesQuery = expensesQuery.Where(a => a.CostCenterId == filter.CostCenterId.Value);
            }

            if (filter.SupplierId.HasValue)
            {
                expensesQuery = expensesQuery.Where(a => a.SupplierId == filter.SupplierId.Value);
            }

            if (filter.PaymentMethod.HasValue)
            {
                expensesQuery = expensesQuery.Where(a => a.PaymentMethod == filter.PaymentMethod.Value);
            }

            var expenses = await expensesQuery.ToListAsync();
            var operatingExpenses = expenses.Sum(e => e.NetAmount);

            var grossProfit = totalRevenue - costOfGoodsSold;
            var operatingIncome = grossProfit - operatingExpenses;
            var netIncome = operatingIncome;

            var expensesByCategory = expenses
                .GroupBy(e => e.Category?.Name ?? "Sem categoria")
                .Select(g => new ExpenseByCategory
                {
                    Category = g.Key,
                    Amount = g.Sum(e => e.NetAmount),
                    Percentage = operatingExpenses > 0 ? (g.Sum(e => e.NetAmount) / operatingExpenses * 100) : 0
                })
                .ToList();

            return new ProfitLossReportDto
            {
                TotalRevenue = totalRevenue,
                CostOfGoodsSold = costOfGoodsSold,
                GrossProfit = grossProfit,
                OperatingExpenses = operatingExpenses,
                OperatingIncome = operatingIncome,
                OtherIncome = 0,
                OtherExpenses = 0,
                NetIncome = netIncome,
                GrossProfitMargin = totalRevenue > 0 ? (grossProfit / totalRevenue * 100) : 0,
                NetProfitMargin = totalRevenue > 0 ? (netIncome / totalRevenue * 100) : 0,
                ExpensesByCategory = expensesByCategory
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório de lucros e perdas");
            throw;
        }
    }

    public async Task<BalanceSheetReportDto> GenerateBalanceSheetReportAsync(FinancialReportFilterDto filter)
    {
        try
        {
            // Calculate current assets (cash + accounts receivable)
            var receivables = await _context.AccountsReceivable
                .Where(a => a.Status == AccountStatus.Pending)
                .SumAsync(a => a.OriginalAmount - a.DiscountAmount + a.InterestAmount + a.FineAmount);

            // Calculate inventory value as fixed asset
            var inventoryValue = await _context.Products
                .Where(p => p.IsActive)
                .SumAsync(p => p.CurrentStock * p.CostPrice);

            // Calculate current liabilities (accounts payable)
            var payables = await _context.AccountsPayable
                .Where(a => a.Status == AccountStatus.Pending)
                .SumAsync(a => a.OriginalAmount - a.DiscountAmount + a.InterestAmount + a.FineAmount);

            // Simple balance sheet calculation
            var currentAssets = receivables; // In real scenario, add cash balance
            var fixedAssets = inventoryValue;
            var totalAssets = currentAssets + fixedAssets;

            var currentLiabilities = payables;
            var longTermLiabilities = 0m; // Not tracked in current system
            var totalLiabilities = currentLiabilities + longTermLiabilities;

            var equity = totalAssets - totalLiabilities;

            return new BalanceSheetReportDto
            {
                TotalAssets = totalAssets,
                CurrentAssets = currentAssets,
                FixedAssets = fixedAssets,
                TotalLiabilities = totalLiabilities,
                CurrentLiabilities = currentLiabilities,
                LongTermLiabilities = longTermLiabilities,
                Equity = equity,
                RetainedEarnings = equity // Simplified
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar balanço patrimonial");
            throw;
        }
    }

    private static DailyClosingItemDto MapReceivableToDailyClosingItem(AccountReceivable account, DateTime reportDate)
    {
        return new DailyClosingItemDto
        {
            Id = account.Id,
            Source = "Conta a Receber",
            Type = "Entrada",
            Counterparty = account.Customer?.Name ?? "Cliente nao informado",
            Description = account.InvoiceNumber ?? account.Notes ?? "Recebimento",
            Category = account.Category?.Name ?? "Sem categoria",
            CostCenter = account.CostCenter?.Name ?? "Sem centro de custo",
            PaymentMethod = account.PaymentMethod.ToDisplayName(),
            Status = account.Status.ToDisplayName(),
            IssueDate = account.IssueDate,
            DueDate = account.DueDate,
            PaymentDate = account.PaymentDate,
            OriginalAmount = account.OriginalAmount,
            DiscountAmount = account.DiscountAmount,
            InterestAmount = account.InterestAmount,
            FineAmount = account.FineAmount,
            NetAmount = account.NetAmount,
            PaidAmount = account.PaidAmount,
            RemainingAmount = account.RemainingAmount,
            IsOverdue = account.DueDate.Date < reportDate && account.Status != AccountStatus.Paid && account.Status != AccountStatus.Cancelled
        };
    }

    private static DailyClosingItemDto MapPayableToDailyClosingItem(AccountPayable account, DateTime reportDate)
    {
        return new DailyClosingItemDto
        {
            Id = account.Id,
            Source = "Conta a Pagar",
            Type = "Saida",
            Counterparty = account.Supplier?.Name ?? "Fornecedor nao informado",
            Description = account.InvoiceNumber ?? account.Notes ?? "Pagamento",
            Category = account.Category?.Name ?? "Sem categoria",
            CostCenter = account.CostCenter?.Name ?? "Sem centro de custo",
            PaymentMethod = account.PaymentMethod.ToDisplayName(),
            Status = account.Status.ToDisplayName(),
            IssueDate = account.IssueDate,
            DueDate = account.DueDate,
            PaymentDate = account.PaymentDate,
            OriginalAmount = account.OriginalAmount,
            DiscountAmount = account.DiscountAmount,
            InterestAmount = account.InterestAmount,
            FineAmount = account.FineAmount,
            NetAmount = account.NetAmount,
            PaidAmount = account.PaidAmount,
            RemainingAmount = account.RemainingAmount,
            IsOverdue = account.DueDate.Date < reportDate && account.Status != AccountStatus.Paid && account.Status != AccountStatus.Cancelled
        };
    }

    private static decimal GetMovementAmount(DailyClosingItemDto item)
    {
        return item.PaidAmount > 0 ? item.PaidAmount : item.NetAmount;
    }

    private static Dictionary<string, decimal> GroupByPaymentMethod(IEnumerable<DailyClosingItemDto> items)
    {
        return items
            .GroupBy(x => x.PaymentMethod)
            .OrderBy(x => x.Key)
            .ToDictionary(g => g.Key, g => g.Sum(GetMovementAmount));
    }

    private static List<DailyClosingGroupDto> GroupByDimension(
        IEnumerable<DailyClosingItemDto> entries,
        IEnumerable<DailyClosingItemDto> exits,
        Func<DailyClosingItemDto, string> keySelector)
    {
        var normalizedEntries = entries.ToList();
        var normalizedExits = exits.ToList();
        var allKeys = normalizedEntries.Select(keySelector)
            .Concat(normalizedExits.Select(keySelector))
            .Distinct()
            .OrderBy(x => x);

        var result = new List<DailyClosingGroupDto>();

        foreach (var key in allKeys)
        {
            var keyEntries = normalizedEntries.Where(x => keySelector(x) == key).ToList();
            var keyExits = normalizedExits.Where(x => keySelector(x) == key).ToList();

            result.Add(new DailyClosingGroupDto
            {
                Group = key,
                Entries = keyEntries.Sum(GetMovementAmount),
                Exits = keyExits.Sum(GetMovementAmount),
                Count = keyEntries.Count + keyExits.Count
            });
        }

        return result;
    }
}
