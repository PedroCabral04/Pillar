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
                .SumAsync(a => a.NetAmount);

            // Calculate inventory value as fixed asset
            var inventoryValue = await _context.Products
                .Where(p => p.IsActive)
                .SumAsync(p => p.CurrentStock * p.CostPrice);

            // Calculate current liabilities (accounts payable)
            var payables = await _context.AccountsPayable
                .Where(a => a.Status == AccountStatus.Pending)
                .SumAsync(a => a.NetAmount);

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
}
