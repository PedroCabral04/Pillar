using erp.Data;
using erp.DTOs.Audit;
using erp.Models.Audit;
using Microsoft.EntityFrameworkCore;

namespace erp.Services.Audit;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;

    public AuditService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<AuditLogDto>> GetEntityHistoryAsync(string entityName, string entityId)
    {
        return await _context.AuditLogs
            .Where(a => a.EntityName == entityName && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .Select(a => MapToDto(a))
            .ToListAsync();
    }

    public async Task<List<AuditLogDto>> GetUserActionsAsync(int userId, int limit = 100)
    {
        return await _context.AuditLogs
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .Select(a => MapToDto(a))
            .ToListAsync();
    }

    public async Task<List<AuditLogDto>> GetRecentChangesAsync(int limit = 50)
    {
        return await _context.AuditLogs
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .Select(a => MapToDto(a))
            .ToListAsync();
    }

    public async Task<AuditLogPagedResultDto> SearchLogsAsync(AuditLogFilterDto filter)
    {
        var query = _context.AuditLogs.AsQueryable();

        // Aplicar filtros
        if (!string.IsNullOrEmpty(filter.EntityName))
        {
            query = query.Where(a => a.EntityName == filter.EntityName);
        }

        if (!string.IsNullOrEmpty(filter.EntityId))
        {
            query = query.Where(a => a.EntityId == filter.EntityId);
        }

        if (filter.UserId.HasValue)
        {
            query = query.Where(a => a.UserId == filter.UserId);
        }

        if (!string.IsNullOrEmpty(filter.Action))
        {
            query = query.Where(a => a.Action.ToString() == filter.Action);
        }

        if (filter.StartDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= filter.EndDate.Value);
        }

        // Contar total
        var totalCount = await query.CountAsync();

        // Aplicar paginação
        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(a => MapToDto(a))
            .ToListAsync();

        return new AuditLogPagedResultDto
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<AuditStatisticsDto> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= endDate.Value);
        }

        var logs = await query.ToListAsync();

        var stats = new AuditStatisticsDto
        {
            TotalLogs = logs.Count,
            TotalCreates = logs.Count(a => a.Action == AuditAction.Create),
            TotalUpdates = logs.Count(a => a.Action == AuditAction.Update),
            TotalDeletes = logs.Count(a => a.Action == AuditAction.Delete),
            LogsByEntity = logs.GroupBy(a => a.EntityName)
                .ToDictionary(g => g.Key, g => g.Count()),
            LogsByUser = logs.Where(a => a.UserName != null)
                .GroupBy(a => a.UserName!)
                .ToDictionary(g => g.Key, g => g.Count()),
            TopUsers = logs.Where(a => a.UserId.HasValue)
                .GroupBy(a => new { a.UserId, a.UserName })
                .Select(g => new TopUserActivityDto
                {
                    UserId = g.Key.UserId!.Value,
                    UserName = g.Key.UserName,
                    ActivityCount = g.Count()
                })
                .OrderByDescending(u => u.ActivityCount)
                .Take(10)
                .ToList(),
            TopEntities = logs.GroupBy(a => a.EntityName)
                .Select(g => new TopEntityActivityDto
                {
                    EntityName = g.Key,
                    ActivityCount = g.Count()
                })
                .OrderByDescending(e => e.ActivityCount)
                .Take(10)
                .ToList()
        };

        return stats;
    }

    public async Task<List<AuditLogDto>> GetLogsByActionAsync(string action, int limit = 100)
    {
        if (!Enum.TryParse<AuditAction>(action, true, out var auditAction))
        {
            return new List<AuditLogDto>();
        }

        return await _context.AuditLogs
            .Where(a => a.Action == auditAction)
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .Select(a => MapToDto(a))
            .ToListAsync();
    }

    public async Task<List<AuditLogDto>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.AuditLogs
            .Where(a => a.Timestamp >= startDate && a.Timestamp <= endDate)
            .OrderByDescending(a => a.Timestamp)
            .Select(a => MapToDto(a))
            .ToListAsync();
    }

    private static AuditLogDto MapToDto(AuditLog log)
    {
        return new AuditLogDto
        {
            Id = log.Id,
            EntityName = log.EntityName,
            EntityId = log.EntityId,
            Action = log.Action.ToString(),
            UserId = log.UserId,
            UserName = log.UserName,
            OldValues = log.OldValues,
            NewValues = log.NewValues,
            ChangedProperties = log.ChangedProperties,
            Timestamp = log.Timestamp,
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent,
            AdditionalInfo = log.AdditionalInfo
        };
    }
}
