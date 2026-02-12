using erp.DAOs.Financial;
using erp.Data;
using erp.DTOs.Financial;
using erp.Models.Financial;
using erp.Models.Identity;
using erp.Models.Sales;
using erp.Models.ServiceOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace erp.Services.Financial;

public class CommissionService : ICommissionService
{
    private readonly ICommissionDao _commissionDao;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CommissionService> _logger;

    public CommissionService(
        ICommissionDao commissionDao,
        ApplicationDbContext context,
        ILogger<CommissionService> logger)
    {
        _commissionDao = commissionDao;
        _context = context;
        _logger = logger;
    }

    public async Task CalculateCommissionsForSaleAsync(int saleId)
    {
        var sale = await _context.Sales
            .Include(s => s.Items)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == saleId);

        if (sale == null)
        {
            _logger.LogWarning("Sale {SaleId} not found for commission calculation", saleId);
            return;
        }

        if (sale.Status != "Finalizada")
        {
            _logger.LogInformation("Sale {SaleId} is not finalized (status: {Status}), skipping commission calculation", saleId, sale.Status);
            return;
        }

        if (sale.UserId == 0)
        {
            _logger.LogWarning("Sale {SaleId} has no associated user, skipping commission calculation", saleId);
            return;
        }

        // Get all product IDs in the sale
        var productIds = sale.Items.Select(i => i.ProductId).ToList();

        // Get products with commission info
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p);

        // Get TenantId from the sale user
        var tenantId = sale.User?.TenantId ?? 0;
        if (tenantId == 0)
        {
            _logger.LogWarning("Sale {SaleId} has user with invalid TenantId (0), commissions may not appear in tenant-scoped queries", saleId);
        }

        var commissionsToAdd = new List<Commission>();

        foreach (var item in sale.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
                continue;

            // Skip if product has no commission
            if (product.CommissionPercent <= 0)
                continue;

            // Calculate profit using sale-time prices: (UnitPrice - CostPrice) * Quantity
            // Use item.UnitPrice (captured at sale time) and item.CostPrice (captured at sale time)
            // Fall back to product.CostPrice for legacy items that don't have CostPrice set
            var costPrice = item.CostPrice > 0 ? item.CostPrice : product.CostPrice;
            var profitPerUnit = item.UnitPrice - costPrice;
            var totalProfit = profitPerUnit * item.Quantity;

            if (totalProfit <= 0)
                continue;

            // Calculate commission: Profit * CommissionPercent / 100
            var commissionAmount = totalProfit * (product.CommissionPercent / 100m);

            // Check if commission already exists for this sale item
            var existing = await _context.Commissions
                .FirstOrDefaultAsync(c => c.SaleItemId == item.Id);

            if (existing != null)
            {
                // Update existing commission
                existing.ProfitAmount = totalProfit;
                existing.CommissionPercent = product.CommissionPercent;
                existing.CommissionAmount = commissionAmount;
                existing.UpdatedAt = DateTime.UtcNow;
                _context.Commissions.Update(existing);
            }
            else
            {
                // Create new commission - add to batch list instead of saving immediately
                var commission = new Commission
                {
                    TenantId = tenantId,
                    SaleId = sale.Id,
                    SaleItemId = item.Id,
                    ProductId = product.Id,
                    UserId = sale.UserId,
                    ProfitAmount = totalProfit,
                    CommissionPercent = product.CommissionPercent,
                    CommissionAmount = commissionAmount,
                    Status = CommissionStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = sale.UserId
                };

                commissionsToAdd.Add(commission);
            }

            _logger.LogInformation(
                "Commission calculated for Sale {SaleId}, Product {ProductName}: Profit={Profit}, Percent={Percent}%, Amount={Amount}",
                saleId, product.Name, totalProfit, product.CommissionPercent, commissionAmount);
        }

        // Batch add all new commissions at once
        if (commissionsToAdd.Count > 0)
        {
            _context.Commissions.AddRange(commissionsToAdd);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<CommissionSummaryDto> GetUserCommissionsByMonthAsync(int userId, int year, int month)
    {
        var commissions = await _commissionDao.GetByUserIdAndMonthAsync(userId, year, month);

        var user = await _context.Users.OfType<ApplicationUser>().FirstOrDefaultAsync(u => u.Id == userId);
        var userName = user?.FullName ?? user?.UserName ?? "Unknown";

        var monthName = new DateTime(year, month, 1).ToString("MMMM", new System.Globalization.CultureInfo("pt-BR"));

        return new CommissionSummaryDto
        {
            UserId = userId,
            UserName = userName,
            Year = year,
            Month = month,
            MonthName = char.ToUpper(monthName[0]) + monthName.Substring(1),
            TotalSales = commissions.Count,
            TotalProfit = commissions.Sum(c => c.ProfitAmount),
            TotalCommission = commissions.Sum(c => c.CommissionAmount),
            PaidCommission = commissions.Where(c => c.Status == CommissionStatus.Paid).Sum(c => c.CommissionAmount),
            PendingCommission = commissions.Where(c => c.Status == CommissionStatus.Pending || c.Status == CommissionStatus.Approved).Sum(c => c.CommissionAmount),
            Commissions = commissions.Select(MapToDto).ToList()
        };
    }

    public async Task<List<CommissionDto>> GetCommissionsAsync(int? userId = null, CommissionStatus? status = null, int? year = null, int? month = null)
    {
        var commissions = await _commissionDao.GetAllAsync(userId, status, year, month);
        return commissions.Select(MapToDto).ToList();
    }

    public async Task MarkCommissionAsPaidAsync(int commissionId, int payrollId)
    {
        var commission = await _commissionDao.GetByIdAsync(commissionId);
        if (commission == null)
            throw new KeyNotFoundException($"Commission {commissionId} not found");

        commission.Status = CommissionStatus.Paid;
        commission.PaidDate = DateTime.UtcNow;
        commission.PayrollId = payrollId;
        commission.UpdatedAt = DateTime.UtcNow;

        await _commissionDao.UpdateAsync(commission);
        _logger.LogInformation("Commission {CommissionId} marked as paid with payroll {PayrollId}", commissionId, payrollId);
    }

    public async Task CancelCommissionsForSaleAsync(int saleId)
    {
        var commissions = await _commissionDao.GetBySaleIdAsync(saleId);

        foreach (var commission in commissions)
        {
            if (commission.Status == CommissionStatus.Paid)
            {
                _logger.LogWarning("Cannot cancel paid commission {CommissionId}", commission.Id);
                continue;
            }

            commission.Status = CommissionStatus.Cancelled;
            commission.UpdatedAt = DateTime.UtcNow;
            await _commissionDao.UpdateAsync(commission);
        }

        _logger.LogInformation("Cancelled {Count} commissions for sale {SaleId}", commissions.Count, saleId);
    }

    public async Task CalculateCommissionsForServiceOrderAsync(int serviceOrderId)
    {
        var order = await _context.ServiceOrders
            .Include(o => o.Items)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == serviceOrderId);

        if (order == null)
        {
            _logger.LogWarning("ServiceOrder {OrderId} not found for commission calculation", serviceOrderId);
            return;
        }

        if (order.Status != "Concluído" && order.Status != "Entregue")
        {
            _logger.LogInformation("ServiceOrder {OrderId} is not completed (status: {Status}), skipping commission calculation", serviceOrderId, order.Status);
            return;
        }

        if (order.UserId == 0)
        {
            _logger.LogWarning("ServiceOrder {OrderId} has no associated user, skipping commission calculation", serviceOrderId);
            return;
        }

        // Get TenantId from the order user
        var tenantId = order.User?.TenantId ?? 0;
        if (tenantId == 0)
        {
            _logger.LogWarning("ServiceOrder {OrderId} has user with invalid TenantId (0)", serviceOrderId);
        }

        var commissionsToAdd = new List<ServiceOrderCommission>();

        foreach (var item in order.Items)
        {
            // Skip if item has no commission
            if (item.CommissionPercent <= 0)
                continue;

            // Calculate profit: Price - CostPrice
            var profit = item.Price - item.CostPrice;
            if (profit <= 0)
                continue;

            // Calculate commission: Profit * CommissionPercent / 100
            var commissionAmount = profit * (item.CommissionPercent / 100m);

            // Check if commission already exists
            var existing = await _context.ServiceOrderCommissions
                .FirstOrDefaultAsync(c => c.ServiceOrderItemId == item.Id);

            if (existing != null)
            {
                existing.ProfitAmount = profit;
                existing.CommissionPercent = item.CommissionPercent;
                existing.CommissionAmount = commissionAmount;
                existing.UpdatedAt = DateTime.UtcNow;
                _context.ServiceOrderCommissions.Update(existing);
            }
            else
            {
                var commission = new ServiceOrderCommission
                {
                    TenantId = tenantId,
                    ServiceOrderId = order.Id,
                    ServiceOrderItemId = item.Id,
                    UserId = order.UserId,
                    ProfitAmount = profit,
                    CommissionPercent = item.CommissionPercent,
                    CommissionAmount = commissionAmount,
                    Status = CommissionStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = order.UserId
                };

                commissionsToAdd.Add(commission);
            }

            _logger.LogInformation(
                "Commission calculated for ServiceOrder {OrderId}, Item {Description}: Profit={Profit}, Percent={Percent}%, Amount={Amount}",
                serviceOrderId, item.Description, profit, item.CommissionPercent, commissionAmount);
        }

        if (commissionsToAdd.Count > 0)
        {
            _context.ServiceOrderCommissions.AddRange(commissionsToAdd);
        }

        await _context.SaveChangesAsync();
    }

    public async Task CancelCommissionsForServiceOrderAsync(int serviceOrderId)
    {
        var commissions = await _context.ServiceOrderCommissions
            .Where(c => c.ServiceOrderId == serviceOrderId)
            .ToListAsync();

        foreach (var commission in commissions)
        {
            if (commission.Status == CommissionStatus.Paid)
            {
                _logger.LogWarning("Cannot cancel paid commission {CommissionId}", commission.Id);
                continue;
            }

            commission.Status = CommissionStatus.Cancelled;
            commission.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Cancelled {Count} commissions for service order {OrderId}", commissions.Count, serviceOrderId);
    }

    private static CommissionDto MapToDto(Commission commission)
    {
        return new CommissionDto
        {
            Id = commission.Id,
            SaleId = commission.SaleId,
            SaleItemId = commission.SaleItemId,
            ProductId = commission.ProductId,
            ProductName = commission.Product?.Name ?? "Unknown",
            UserId = commission.UserId,
            UserName = commission.User?.FullName ?? commission.User?.UserName ?? "Unknown",
            ProfitAmount = commission.ProfitAmount,
            CommissionPercent = commission.CommissionPercent,
            CommissionAmount = commission.CommissionAmount,
            Status = commission.Status,
            StatusDescription = commission.Status switch
            {
                CommissionStatus.Pending => "Pendente",
                CommissionStatus.Approved => "Aprovada",
                CommissionStatus.Paid => "Paga",
                CommissionStatus.Cancelled => "Cancelada",
                _ => "Desconhecido"
            },
            PaidDate = commission.PaidDate,
            PayrollId = commission.PayrollId,
            CreatedAt = commission.CreatedAt
        };
    }
}
