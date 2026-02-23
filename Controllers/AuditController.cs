using erp.DTOs.Audit;
using erp.Services.Audit;
using erp.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using erp.Models.Audit;

namespace erp.Controllers;

[Authorize]
[ApiController]
[Route("api/auditoria")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(IAuditService auditService, ILogger<AuditController> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Obtém o histórico completo de uma entidade específica
    /// </summary>
    /// <param name="entityName">Nome da entidade (ex: Product, Customer)</param>
    /// <param name="entityId">ID da entidade</param>
    [HttpGet("entity/{entityName}/{entityId}")]
    [ProducesResponseType(typeof(List<AuditLogDto>), 200)]
    public async Task<IActionResult> GetEntityHistory(string entityName, string entityId)
    {
        try
        {
            var history = await _auditService.GetEntityHistoryAsync(entityName, entityId);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar histórico da entidade {EntityName} com ID {EntityId}", entityName, entityId);
            return StatusCode(500, new { message = "Erro ao buscar histórico da entidade" });
        }
    }

    /// <summary>
    /// Obtém todas as ações realizadas por um usuário
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="limit">Limite de registros (padrão: 100)</param>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(List<AuditLogDto>), 200)]
    public async Task<IActionResult> GetUserActions(int userId, [FromQuery] int limit = 100)
    {
        try
        {
            var actions = await _auditService.GetUserActionsAsync(userId, limit);
            return Ok(actions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar ações do usuário {UserId}", userId);
            return StatusCode(500, new { message = "Erro ao buscar ações do usuário" });
        }
    }

    /// <summary>
    /// Obtém as mudanças mais recentes no sistema
    /// </summary>
    /// <param name="limit">Limite de registros (padrão: 50)</param>
    [HttpGet("recent")]
    [ProducesResponseType(typeof(List<AuditLogDto>), 200)]
    public async Task<IActionResult> GetRecentChanges([FromQuery] int limit = 50)
    {
        try
        {
            var changes = await _auditService.GetRecentChangesAsync(limit);
            return Ok(changes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar mudanças recentes");
            return StatusCode(500, new { message = "Erro ao buscar mudanças recentes" });
        }
    }

    /// <summary>
    /// Busca logs com filtros avançados e paginação
    /// </summary>
    [HttpPost("search")]
    [ProducesResponseType(typeof(AuditLogPagedResultDto), 200)]
    public async Task<IActionResult> SearchLogs([FromBody] AuditLogFilterDto filter)
    {
        try
        {
            var result = await _auditService.SearchLogsAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar logs com filtros");
            return StatusCode(500, new { message = "Erro ao buscar logs" });
        }
    }

    /// <summary>
    /// Obtém estatísticas de auditoria
    /// </summary>
    /// <param name="startDate">Data inicial (opcional)</param>
    /// <param name="endDate">Data final (opcional)</param>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(AuditStatisticsDto), 200)]
    public async Task<IActionResult> GetStatistics([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var stats = await _auditService.GetStatisticsAsync(startDate, endDate);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar estatísticas de auditoria");
            return StatusCode(500, new { message = "Erro ao buscar estatísticas" });
        }
    }

    /// <summary>
    /// Obtém logs por tipo de ação
    /// </summary>
    /// <param name="action">Tipo de ação (Create, Update, Delete)</param>
    /// <param name="limit">Limite de registros (padrão: 100)</param>
    [HttpGet("action/{action}")]
    [ProducesResponseType(typeof(List<AuditLogDto>), 200)]
    public async Task<IActionResult> GetLogsByAction(string action, [FromQuery] int limit = 100)
    {
        try
        {
            var logs = await _auditService.GetLogsByActionAsync(action, limit);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar logs por ação {Action}", action);
            return StatusCode(500, new { message = "Erro ao buscar logs por ação" });
        }
    }

    /// <summary>
    /// Obtém logs de um período específico
    /// </summary>
    /// <param name="startDate">Data inicial</param>
    /// <param name="endDate">Data final</param>
    [HttpGet("daterange")]
    [ProducesResponseType(typeof(List<AuditLogDto>), 200)]
    public async Task<IActionResult> GetLogsByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            if (startDate > endDate)
            {
                return BadRequest(new { message = "Data inicial não pode ser maior que data final" });
            }

            var logs = await _auditService.GetLogsByDateRangeAsync(startDate, endDate);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar logs por período");
            return StatusCode(500, new { message = "Erro ao buscar logs por período" });
        }
    }

    /// <summary>
    /// Obtém relatório de acesso a dados sensíveis para compliance LGPD/GDPR
    /// </summary>
    /// <param name="startDate">Data inicial (opcional)</param>
    /// <param name="endDate">Data final (opcional)</param>
    /// <param name="minSensitivity">Nível mínimo de sensibilidade (opcional)</param>
    [HttpGet("sensitive-data-access")]
    [ProducesResponseType(typeof(List<DataAccessReportDto>), 200)]
    [Authorize(Roles = RoleNames.AdminTenantOrSuperAdmin)] // Apenas admins podem ver relatórios de acesso
    public async Task<IActionResult> GetSensitiveDataAccessReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] DataSensitivity? minSensitivity = null)
    {
        try
        {
            // Converte datas para UTC se necessário (PostgreSQL exige UTC)
            DateTime? utcStart = startDate.HasValue && startDate.Value.Kind != DateTimeKind.Utc
                ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc)
                : startDate;
                
            DateTime? utcEnd = endDate.HasValue && endDate.Value.Kind != DateTimeKind.Utc
                ? DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc)
                : endDate;
            
            var report = await _auditService.GetSensitiveDataAccessReportAsync(utcStart, utcEnd, minSensitivity);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório de acesso a dados sensíveis");
            return StatusCode(500, new { message = "Erro ao gerar relatório de acesso" });
        }
    }

    /// <summary>
    /// Obtém histórico de quem acessou uma entidade específica
    /// </summary>
    /// <param name="entityName">Nome da entidade</param>
    /// <param name="entityId">ID da entidade</param>
    [HttpGet("entity-access/{entityName}/{entityId}")]
    [ProducesResponseType(typeof(List<AuditLogDto>), 200)]
    public async Task<IActionResult> GetEntityAccessHistory(string entityName, string entityId)
    {
        try
        {
            var history = await _auditService.GetEntityAccessHistoryAsync(entityName, entityId);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar histórico de acesso da entidade {EntityName} ID {EntityId}", entityName, entityId);
            return StatusCode(500, new { message = "Erro ao buscar histórico de acesso" });
        }
    }
}
