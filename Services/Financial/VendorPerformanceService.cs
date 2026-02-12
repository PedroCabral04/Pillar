using erp.DAOs.Financial;
using erp.Data;
using erp.DTOs.Financial;
using erp.Models.Financial;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace erp.Services.Financial;

public class VendorPerformanceService : IVendorPerformanceService
{
    private readonly IVendorPerformanceDao _vendorPerformanceDao;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VendorPerformanceService> _logger;

    public VendorPerformanceService(
        IVendorPerformanceDao vendorPerformanceDao,
        ApplicationDbContext context,
        ILogger<VendorPerformanceService> logger)
    {
        _vendorPerformanceDao = vendorPerformanceDao;
        _context = context;
        _logger = logger;
    }

    public async Task<VendorPerformanceDto?> GetByUserAndPeriodAsync(int userId, int year, int month)
    {
        var performance = await _vendorPerformanceDao.GetByUserAndPeriodAsync(userId, year, month);
        return performance == null ? null : MapToDto(performance);
    }

    public async Task<List<VendorPerformanceDto>> GetByTenantAndPeriodAsync(int tenantId, int year, int month)
    {
        var performances = await _vendorPerformanceDao.GetByTenantAndPeriodAsync(tenantId, year, month);
        return performances.Select(MapToDto).ToList();
    }

    public async Task<List<VendorPerformanceDto>> GetTopPerformersAsync(int tenantId, int year, int month, int topN)
    {
        var performers = await _vendorPerformanceDao.GetTopPerformersAsync(tenantId, year, month, topN);
        return performers.Select(MapToDto).ToList();
    }

    public async Task<VendorPerformanceDto> CalculatePerformanceAsync(int userId, int year, int month)
    {
        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1);

        // Get user info
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.TenantId == null)
            throw new InvalidOperationException($"User {userId} not found or has no tenant");

        // Calculate sales metrics
        var salesMetrics = await _context.Sales
            .Where(s => s.UserId == userId && s.SaleDate >= startDate && s.SaleDate < endDate && s.Status == "Finalizada")
            .GroupBy(s => 1)
            .Select(g => new
            {
                TotalSalesCount = g.Count(),
                TotalSalesAmount = g.Sum(s => s.NetAmount),
                TotalProfitAmount = g.Sum(s => s.Items.Sum(i => (i.UnitPrice - i.CostPrice) * i.Quantity))
            })
            .FirstOrDefaultAsync();

        // Calculate commission metrics
        var commissions = await _context.Commissions
            .Where(c => c.UserId == userId && c.CreatedAt >= startDate && c.CreatedAt < endDate)
            .GroupBy(c => 1)
            .Select(g => new
            {
                TotalCommissionEarned = g.Sum(c => c.CommissionAmount),
                TotalCommissionPaid = g.Where(c => c.Status == CommissionStatus.Paid).Sum(c => c.CommissionAmount),
                TotalCommissionPending = g.Where(c => c.Status == CommissionStatus.Pending || c.Status == CommissionStatus.Approved).Sum(c => c.CommissionAmount)
            })
            .FirstOrDefaultAsync();

        // Get sales goal
        var goal = await _context.SalesGoals
            .FirstOrDefaultAsync(g => g.UserId == userId && g.Year == year && g.Month == month);

        // Check goal achievement
        decimal? salesGoalTarget = goal?.TargetSalesAmount;
        decimal? salesGoalAchievementPercent = null;
        bool salesGoalAchieved = false;

        if (salesGoalTarget.HasValue && salesGoalTarget.Value > 0 && salesMetrics != null)
        {
            salesGoalAchievementPercent = (salesMetrics.TotalSalesAmount / salesGoalTarget.Value) * 100m;
            salesGoalAchieved = salesGoalAchievementPercent >= 100m;
        }

        // Create or update performance record
        var existing = await _vendorPerformanceDao.GetByUserAndPeriodAsync(userId, year, month);

        var performance = existing ?? new VendorPerformance
        {
            TenantId = user.TenantId.Value,
            UserId = userId,
            Year = year,
            Month = month,
            CreatedAt = DateTime.UtcNow
        };

        performance.TotalSalesCount = salesMetrics?.TotalSalesCount ?? 0;
        performance.TotalSalesAmount = salesMetrics?.TotalSalesAmount ?? 0;
        performance.TotalProfitAmount = salesMetrics?.TotalProfitAmount ?? 0;
        performance.TotalCommissionEarned = commissions?.TotalCommissionEarned ?? 0;
        performance.TotalCommissionPaid = commissions?.TotalCommissionPaid ?? 0;
        performance.TotalCommissionPending = commissions?.TotalCommissionPending ?? 0;
        performance.SalesGoalTarget = salesGoalTarget;
        performance.SalesGoalAchievementPercent = salesGoalAchievementPercent;
        performance.SalesGoalAchieved = salesGoalAchieved;
        performance.BonusCommissionEarned = 0; // Calculate based on goal achievement
        performance.LastCalculatedAt = DateTime.UtcNow;
        performance.UpdatedAt = DateTime.UtcNow;

        var result = existing == null
            ? await _vendorPerformanceDao.CreateAsync(performance)
            : await _vendorPerformanceDao.UpdateAsync(performance);

        return MapToDto(result);
    }

    public async Task RecalculatePeriodAsync(int tenantId, int year, int month)
    {
        // Get all users with sales/commissions in this period
        var userIds = await _context.Sales
            .Where(s => s.TenantId == tenantId &&
                       s.SaleDate >= new DateTime(year, month, 1) &&
                       s.SaleDate < new DateTime(year, month, 1).AddMonths(1))
            .Select(s => s.UserId)
            .Distinct()
            .ToListAsync();

        foreach (var userId in userIds)
        {
            try
            {
                await CalculatePerformanceAsync(userId, year, month);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao calcular performance para usuário {UserId}", userId);
            }
        }

        _logger.LogInformation("Performance recalculada para {Count} vendedores no período {Month}/{Year}", userIds.Count, month, year);
    }

    private static VendorPerformanceDto MapToDto(VendorPerformance performance)
    {
        var monthName = new DateTime(performance.Year, performance.Month, 1).ToString("MMMM", new System.Globalization.CultureInfo("pt-BR"));
        return new VendorPerformanceDto
        {
            Id = performance.Id,
            UserId = performance.UserId,
            UserName = performance.User?.FullName ?? performance.User?.UserName ?? "N/A",
            Year = performance.Year,
            Month = performance.Month,
            MonthName = char.ToUpper(monthName[0]) + monthName.Substring(1),
            TotalSalesCount = performance.TotalSalesCount,
            TotalSalesAmount = performance.TotalSalesAmount,
            TotalProfitAmount = performance.TotalProfitAmount,
            TotalCommissionEarned = performance.TotalCommissionEarned,
            TotalCommissionPaid = performance.TotalCommissionPaid,
            TotalCommissionPending = performance.TotalCommissionPending,
            BonusCommissionEarned = performance.BonusCommissionEarned,
            SalesGoalTarget = performance.SalesGoalTarget,
            SalesGoalAchievementPercent = performance.SalesGoalAchievementPercent,
            SalesGoalAchieved = performance.SalesGoalAchieved,
            LastCalculatedAt = performance.LastCalculatedAt
        };
    }
}
