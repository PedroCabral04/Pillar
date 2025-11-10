using erp.Data;
using Microsoft.EntityFrameworkCore;

namespace erp.Services.Audit;

public interface IAuditAnalyticsService
{
    Task<AuditAnalytics> GetAnalyticsAsync(DateTime startDate, DateTime endDate);
    Task<List<UserActivitySummary>> GetTopActiveUsersAsync(int limit = 10);
    Task<List<EntityActivitySummary>> GetMostModifiedEntitiesAsync(int limit = 10);
    Task<List<HourlyActivity>> GetActivityByHourAsync(DateTime date);
}

public class AuditAnalyticsService : IAuditAnalyticsService
{
    private readonly ApplicationDbContext _context;

    public AuditAnalyticsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AuditAnalytics> GetAnalyticsAsync(DateTime startDate, DateTime endDate)
    {
        var logs = await _context.AuditLogs
            .Where(a => a.Timestamp >= startDate && a.Timestamp <= endDate)
            .ToListAsync();

        return new AuditAnalytics
        {
            TotalActions = logs.Count,
            Creates = logs.Count(l => l.Action == Models.Audit.AuditAction.Create),
            Updates = logs.Count(l => l.Action == Models.Audit.AuditAction.Update),
            Deletes = logs.Count(l => l.Action == Models.Audit.AuditAction.Delete),
            UniqueUsers = logs.Select(l => l.UserId).Distinct().Count(),
            UniqueEntities = logs.Select(l => l.EntityName).Distinct().Count(),
            MostActiveDay = logs.GroupBy(l => l.Timestamp.Date)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key ?? DateTime.MinValue,
            AverageActionsPerDay = logs.Any() 
                ? logs.Count / ((endDate - startDate).Days + 1) 
                : 0
        };
    }

    public async Task<List<UserActivitySummary>> GetTopActiveUsersAsync(int limit = 10)
    {
        return await _context.AuditLogs
            .Where(a => a.UserId != null)
            .GroupBy(a => new { a.UserId, a.UserName })
            .Select(g => new UserActivitySummary
            {
                UserId = g.Key.UserId!.Value,
                UserName = g.Key.UserName ?? "Desconhecido",
                TotalActions = g.Count(),
                Creates = g.Count(a => a.Action == Models.Audit.AuditAction.Create),
                Updates = g.Count(a => a.Action == Models.Audit.AuditAction.Update),
                Deletes = g.Count(a => a.Action == Models.Audit.AuditAction.Delete),
                LastActivity = g.Max(a => a.Timestamp)
            })
            .OrderByDescending(u => u.TotalActions)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<EntityActivitySummary>> GetMostModifiedEntitiesAsync(int limit = 10)
    {
        return await _context.AuditLogs
            .GroupBy(a => new { a.EntityName, a.EntityId })
            .Select(g => new EntityActivitySummary
            {
                EntityName = g.Key.EntityName,
                EntityId = g.Key.EntityId,
                TotalModifications = g.Count(),
                Creates = g.Count(a => a.Action == Models.Audit.AuditAction.Create),
                Updates = g.Count(a => a.Action == Models.Audit.AuditAction.Update),
                Deletes = g.Count(a => a.Action == Models.Audit.AuditAction.Delete),
                FirstModification = g.Min(a => a.Timestamp),
                LastModification = g.Max(a => a.Timestamp)
            })
            .OrderByDescending(e => e.TotalModifications)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<HourlyActivity>> GetActivityByHourAsync(DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        return await _context.AuditLogs
            .Where(a => a.Timestamp >= startOfDay && a.Timestamp < endOfDay)
            .GroupBy(a => a.Timestamp.Hour)
            .Select(g => new HourlyActivity
            {
                Hour = g.Key,
                ActionCount = g.Count(),
                UniqueUsers = g.Select(a => a.UserId).Distinct().Count()
            })
            .OrderBy(h => h.Hour)
            .ToListAsync();
    }
}

// DTOs
public class AuditAnalytics
{
    public int TotalActions { get; set; }
    public int Creates { get; set; }
    public int Updates { get; set; }
    public int Deletes { get; set; }
    public int UniqueUsers { get; set; }
    public int UniqueEntities { get; set; }
    public DateTime MostActiveDay { get; set; }
    public int AverageActionsPerDay { get; set; }
}

public class UserActivitySummary
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int TotalActions { get; set; }
    public int Creates { get; set; }
    public int Updates { get; set; }
    public int Deletes { get; set; }
    public DateTime LastActivity { get; set; }
}

public class EntityActivitySummary
{
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public int TotalModifications { get; set; }
    public int Creates { get; set; }
    public int Updates { get; set; }
    public int Deletes { get; set; }
    public DateTime FirstModification { get; set; }
    public DateTime LastModification { get; set; }
}

public class HourlyActivity
{
    public int Hour { get; set; }
    public int ActionCount { get; set; }
    public int UniqueUsers { get; set; }
}
