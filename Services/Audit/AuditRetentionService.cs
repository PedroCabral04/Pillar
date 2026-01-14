using erp.Data;
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
        
        var logsToArchive = await _context.AuditLogs
            .Where(a => a.Timestamp < cutoffDate)
            .Take(10000) // Processar em lotes
            .ToListAsync();

        if (!logsToArchive.Any())
        {
            _logger.LogInformation("Nenhum log para arquivar");
            return;
        }

        // NOTE: Archiving not yet implemented.
        // Current implementation only identifies logs to archive.
        // Future implementation should:
        // - Copy logs to AuditLogsArchive table
        // - Or export to cold storage (S3/Azure Blob)
        // - Mark original logs as archived

        // For now, we just log the count
        _logger.LogInformation(
            "Identificados {Count} logs para arquivar anteriores a {Date}. Implementação de arquivamento pendente.",
            logsToArchive.Count,
            cutoffDate);
    }

    /// <summary>
    /// Deleta logs muito antigos (após arquivamento)
    /// </summary>
    public async Task DeleteArchivedLogsAsync(int daysToKeep = 1825)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        
        var deletedCount = await _context.AuditLogs
            .Where(a => a.Timestamp < cutoffDate)
            .ExecuteDeleteAsync();

        _logger.LogWarning(
            "Deletados {Count} logs anteriores a {Date}", 
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
