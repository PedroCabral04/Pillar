using erp.Data;
using erp.Models.Financial;
using Microsoft.EntityFrameworkCore;

namespace erp.DAOs.Financial;

public interface ISalesGoalDao
{
    Task<SalesGoal?> GetByIdAsync(int id);
    Task<List<SalesGoal>> GetByUserIdAsync(int userId);
    Task<SalesGoal?> GetByUserAndPeriodAsync(int userId, int year, int month);
    Task<List<SalesGoal>> GetByTenantAndPeriodAsync(int tenantId, int year, int month);
    Task<SalesGoal> CreateAsync(SalesGoal goal);
    Task<SalesGoal> UpdateAsync(SalesGoal goal);
    Task DeleteAsync(int id);
}

public class SalesGoalDao : ISalesGoalDao
{
    private readonly ApplicationDbContext _context;

    public SalesGoalDao(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SalesGoal?> GetByIdAsync(int id)
    {
        return await _context.SalesGoals
            .Include(g => g.User)
            .Include(g => g.CreatedByUser)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<List<SalesGoal>> GetByUserIdAsync(int userId)
    {
        return await _context.SalesGoals
            .Include(g => g.CreatedByUser)
            .Where(g => g.UserId == userId)
            .OrderByDescending(g => g.Year)
            .ThenByDescending(g => g.Month)
            .ToListAsync();
    }

    public async Task<SalesGoal?> GetByUserAndPeriodAsync(int userId, int year, int month)
    {
        return await _context.SalesGoals
            .FirstOrDefaultAsync(g => g.UserId == userId && g.Year == year && g.Month == month);
    }

    public async Task<List<SalesGoal>> GetByTenantAndPeriodAsync(int tenantId, int year, int month)
    {
        return await _context.SalesGoals
            .Include(g => g.User)
            .Include(g => g.CreatedByUser)
            .Where(g => g.TenantId == tenantId && g.Year == year && g.Month == month)
            .ToListAsync();
    }

    public async Task<SalesGoal> CreateAsync(SalesGoal goal)
    {
        _context.SalesGoals.Add(goal);
        await _context.SaveChangesAsync();
        return goal;
    }

    public async Task<SalesGoal> UpdateAsync(SalesGoal goal)
    {
        _context.SalesGoals.Update(goal);
        await _context.SaveChangesAsync();
        return goal;
    }

    public async Task DeleteAsync(int id)
    {
        var goal = await _context.SalesGoals.FindAsync(id);
        if (goal != null)
        {
            _context.SalesGoals.Remove(goal);
            await _context.SaveChangesAsync();
        }
    }
}
