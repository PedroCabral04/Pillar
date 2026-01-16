using erp.Data;
using erp.Models.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace erp.Services.Audit;

/// <summary>
/// Serviço para gerenciar retenção e arquivamento de logs de auditoria
/// </summary>
public interface IAuditRetentionService
{
    Task ArchiveOldLogsAsync(int daysToKeep = 365);
    Task DeleteArchivedLogsAsync(int daysToKeep = 1825); // 5 anos
    Task<long> GetStorageSizeAsync();
}

public class AuditRetentionService : IAuditRetentionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditRetentionService> _logger;
    private const int BatchSize = 5000;

    public AuditRetentionService(
        ApplicationDbContext context,
        ILogger<AuditRetentionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Arquiva logs antigos para uma tabela separada (cold storage)
    /// </summary>
    public async Task ArchiveOldLogsAsync(int daysToKeep = 365)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        var archivedAt = DateTime.UtcNow;

        int totalArchived = 0;
        int batchNumber = 0;

        while (true)
        {
            batchNumber++;

            // Get a batch of logs to archive
            var logsToArchive = await _context.AuditLogs
                .Where(a => a.Timestamp < cutoffDate)
                .OrderBy(a => a.Timestamp)
                .Take(BatchSize)
                .ToListAsync();

            if (!logsToArchive.Any())
            {
                break;
            }

            // Create archive entries
            var archiveEntries = logsToArchive.Select(log => new AuditLogArchive
            {
                OriginalAuditLogId = log.Id,
                TenantId = log.TenantId,
                EntityName = log.EntityName,
                EntityId = log.EntityId,
                EntityDescription = log.EntityDescription,
                Action = log.Action,
                UserId = log.UserId,
                UserName = log.UserName,
                OldValues = log.OldValues,
                NewValues = log.NewValues,
                ChangedProperties = log.ChangedProperties,
                References = log.References,
                Timestamp = log.Timestamp,
                ArchivedAt = archivedAt,
                IpAddress = log.IpAddress,
                UserAgent = log.UserAgent,
                AdditionalInfo = log.AdditionalInfo
            }).ToList();

            // Add archive entries to context
            await _context.AuditLogArchives.AddRangeAsync(archiveEntries);

            // Remove original logs
            _context.AuditLogs.RemoveRange(logsToArchive);

            // Save changes for this batch
            await _context.SaveChangesAsync();

            totalArchived += logsToArchive.Count;
            _logger.LogInformation(
                "Arquivamento batch #{BatchNumber}: {Count} logs arquivados (total: {Total})",
                batchNumber,
                logsToArchive.Count,
                totalArchived);
        }

        if (totalArchived == 0)
        {
            _logger.LogInformation("Nenhum log para arquivar anterior a {Date}", cutoffDate);
        }
        else
        {
            _logger.LogInformation(
                "Arquivamento concluído: {TotalCount} logs arquivados para a tabela AuditLogArchives",
                totalArchived);
        }
    }

    /// <summary>
    /// Deleta logs muito antigos da tabela de arquivo (após período de retenção extendido)
    /// </summary>
    public async Task DeleteArchivedLogsAsync(int daysToKeep = 1825)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);

        // Delete from archive table based on archived date, not original timestamp
        var deletedCount = await _context.AuditLogArchives
            .Where(a => a.ArchivedAt < cutoffDate)
            .ExecuteDeleteAsync();

        _logger.LogWarning(
            "Deletados {Count} logs arquivados com arquivamento anterior a {Date}",
            deletedCount,
            cutoffDate);
    }

    /// <summary>
    /// Calcula tamanho estimado da tabela de auditoria
    /// </summary>
    public async Task<long> GetStorageSizeAsync()
    {
        // PostgreSQL specific query
        var sql = @"
            SELECT pg_total_relation_size('""AuditLogs""') as size_bytes";
        
        var result = await _context.Database
            .SqlQueryRaw<long>(sql)
            .FirstOrDefaultAsync();

        return result;
    }
}
