using erp.DAOs.Financial;
using erp.DTOs.Financial;
using erp.Models.Financial;
using Microsoft.Extensions.Logging;

namespace erp.Services.Financial;

public class SalesGoalService : ISalesGoalService
{
    private readonly ISalesGoalDao _salesGoalDao;
    private readonly ILogger<SalesGoalService> _logger;

    public SalesGoalService(ISalesGoalDao salesGoalDao, ILogger<SalesGoalService> logger)
    {
        _salesGoalDao = salesGoalDao;
        _logger = logger;
    }

    public async Task<List<SalesGoalDto>> GetByUserIdAsync(int userId)
    {
        var goals = await _salesGoalDao.GetByUserIdAsync(userId);
        return goals.Select(MapToDto).ToList();
    }

    public async Task<SalesGoalDto?> GetByUserAndPeriodAsync(int userId, int year, int month)
    {
        var goal = await _salesGoalDao.GetByUserAndPeriodAsync(userId, year, month);
        return goal == null ? null : MapToDto(goal);
    }

    public async Task<SalesGoalDto> CreateAsync(CreateSalesGoalDto dto)
    {
        // Check if goal already exists for this period
        var existing = await _salesGoalDao.GetByUserAndPeriodAsync(dto.UserId, dto.Year, dto.Month);
        if (existing != null)
        {
            throw new InvalidOperationException($"Já existe uma meta para o usuário no período {dto.Month}/{dto.Year}");
        }

        var goal = new SalesGoal
        {
            TenantId = dto.TenantId,
            UserId = dto.UserId,
            Year = dto.Year,
            Month = dto.Month,
            TargetSalesAmount = dto.TargetSalesAmount,
            TargetProfitAmount = dto.TargetProfitAmount,
            TargetSalesCount = dto.TargetSalesCount,
            BonusCommissionPercent = dto.BonusCommissionPercent,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = dto.CreatedByUserId
        };

        var created = await _salesGoalDao.CreateAsync(goal);
        _logger.LogInformation("Meta de vendas criada para usuário {UserId} no período {Month}/{Year}", dto.UserId, dto.Month, dto.Year);

        return MapToDto(created);
    }

    public async Task<SalesGoalDto> UpdateAsync(int id, UpdateSalesGoalDto dto)
    {
        var goal = await _salesGoalDao.GetByIdAsync(id);
        if (goal == null)
            throw new KeyNotFoundException($"Meta {id} não encontrada");

        goal.TargetSalesAmount = dto.TargetSalesAmount;
        goal.TargetProfitAmount = dto.TargetProfitAmount;
        goal.TargetSalesCount = dto.TargetSalesCount;
        goal.BonusCommissionPercent = dto.BonusCommissionPercent;
        goal.Notes = dto.Notes;
        goal.UpdatedAt = DateTime.UtcNow;

        var updated = await _salesGoalDao.UpdateAsync(goal);
        return MapToDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        await _salesGoalDao.DeleteAsync(id);
        _logger.LogInformation("Meta de vendas {Id} excluída", id);
    }

    public async Task<List<SalesGoalDto>> GetByTenantAndPeriodAsync(int tenantId, int year, int month)
    {
        var goals = await _salesGoalDao.GetByTenantAndPeriodAsync(tenantId, year, month);
        return goals.Select(MapToDto).ToList();
    }

    private static SalesGoalDto MapToDto(SalesGoal goal)
    {
        var monthName = new DateTime(goal.Year, goal.Month, 1).ToString("MMMM", new System.Globalization.CultureInfo("pt-BR"));
        return new SalesGoalDto
        {
            Id = goal.Id,
            UserId = goal.UserId,
            UserName = goal.User?.FullName ?? goal.User?.UserName ?? "N/A",
            Year = goal.Year,
            Month = goal.Month,
            MonthName = char.ToUpper(monthName[0]) + monthName.Substring(1),
            TargetSalesAmount = goal.TargetSalesAmount,
            TargetProfitAmount = goal.TargetProfitAmount,
            TargetSalesCount = goal.TargetSalesCount,
            BonusCommissionPercent = goal.BonusCommissionPercent,
            Notes = goal.Notes
        };
    }
}
