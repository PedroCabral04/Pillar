using erp.DTOs.Audit;
using erp.Models.Audit;

namespace erp.Services.Audit;

public interface IAuditService
{
    /// <summary>
    /// Obtém o histórico completo de uma entidade específica
    /// </summary>
    Task<List<AuditLogDto>> GetEntityHistoryAsync(string entityName, string entityId);
    
    /// <summary>
    /// Obtém todas as ações realizadas por um usuário
    /// </summary>
    Task<List<AuditLogDto>> GetUserActionsAsync(int userId, int limit = 100);
    
    /// <summary>
    /// Obtém as mudanças mais recentes no sistema
    /// </summary>
    Task<List<AuditLogDto>> GetRecentChangesAsync(int limit = 50);
    
    /// <summary>
    /// Busca logs com filtros avançados e paginação
    /// </summary>
    Task<AuditLogPagedResultDto> SearchLogsAsync(AuditLogFilterDto filter);
    
    /// <summary>
    /// Obtém estatísticas de auditoria
    /// </summary>
    Task<AuditStatisticsDto> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
    
    /// <summary>
    /// Obtém logs por tipo de ação
    /// </summary>
    Task<List<AuditLogDto>> GetLogsByActionAsync(string action, int limit = 100);
    
    /// <summary>
    /// Obtém logs de um período específico
    /// </summary>
    Task<List<AuditLogDto>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Obtém relatório de acesso a dados sensíveis (LGPD/GDPR compliance)
    /// </summary>
    Task<List<DataAccessReportDto>> GetSensitiveDataAccessReportAsync(DateTime? startDate = null, DateTime? endDate = null, DataSensitivity? minSensitivity = null);
    
    /// <summary>
    /// Obtém todos os acessos a uma entidade específica
    /// </summary>
    Task<List<AuditLogDto>> GetEntityAccessHistoryAsync(string entityName, string entityId);
}

public class AuditStatisticsDto
{
    public int TotalLogs { get; set; }
    public int TotalCreates { get; set; }
    public int TotalUpdates { get; set; }
    public int TotalDeletes { get; set; }
    public Dictionary<string, int> LogsByEntity { get; set; } = new();
    public Dictionary<string, int> LogsByUser { get; set; } = new();
    public List<TopUserActivityDto> TopUsers { get; set; } = new();
    public List<TopEntityActivityDto> TopEntities { get; set; } = new();
}

public class TopUserActivityDto
{
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public int ActivityCount { get; set; }
}

public class TopEntityActivityDto
{
    public string EntityName { get; set; } = string.Empty;
    public int ActivityCount { get; set; }
}
