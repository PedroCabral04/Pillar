using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using erp.DTOs.Financial;
using erp.Models.Financial;
using erp.Services.Financial;
using System.Security.Claims;

namespace erp.Controllers;

/// <summary>
/// Controller para gerenciamento de comissões
/// </summary>
[ApiController]
[Route("api/comissoes")]
[Authorize]
public class CommissionsController : ControllerBase
{
    private readonly ICommissionService _commissionService;
    private readonly ILogger<CommissionsController> _logger;

    public CommissionsController(ICommissionService commissionService, ILogger<CommissionsController> logger)
    {
        _commissionService = commissionService;
        _logger = logger;
    }

    /// <summary>
    /// Obtém todas as comissões com filtros opcionais
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<CommissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<CommissionDto>>> GetCommissions(
        [FromQuery] int? userId = null,
        [FromQuery] int? status = null,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null)
    {
        try
        {
            CommissionStatus? commissionStatus = status.HasValue ? (CommissionStatus)status.Value : null;
            var commissions = await _commissionService.GetCommissionsAsync(userId, commissionStatus, year, month);
            return Ok(commissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting commissions");
            return StatusCode(500, "Erro ao buscar comissões");
        }
    }

    /// <summary>
    /// Obtém resumo de comissões de um usuário em um mês específico
    /// </summary>
    [HttpGet("user/{userId}/summary")]
    [ProducesResponseType(typeof(CommissionSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CommissionSummaryDto>> GetUserCommissionSummary(
        int userId,
        [FromQuery] int year,
        [FromQuery] int month)
    {
        try
        {
            var summary = await _commissionService.GetUserCommissionsByMonthAsync(userId, year, month);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting commission summary for user {UserId}", userId);
            return StatusCode(500, "Erro ao buscar resumo de comissões");
        }
    }

    /// <summary>
    /// Obtém resumo de comissões do usuário atual em um mês específico
    /// </summary>
    [HttpGet("my-summary")]
    [ProducesResponseType(typeof(CommissionSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CommissionSummaryDto>> GetMyCommissionSummary(
        [FromQuery] int year,
        [FromQuery] int month)
    {
        try
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var summary = await _commissionService.GetUserCommissionsByMonthAsync(userId, year, month);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting my commission summary");
            return StatusCode(500, "Erro ao buscar resumo de comissões");
        }
    }

    /// <summary>
    /// Marca uma comissão como paga
    /// </summary>
    [HttpPut("{id}/mark-paid")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> MarkAsPaid(int id, [FromQuery] int payrollId)
    {
        try
        {
            await _commissionService.MarkCommissionAsPaidAsync(id, payrollId);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Comissão {id} não encontrada");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking commission {CommissionId} as paid", id);
            return StatusCode(500, "Erro ao marcar comissão como paga");
        }
    }
}
