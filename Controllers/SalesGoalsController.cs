using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using erp.DTOs.Financial;
using erp.Services.Financial;

namespace erp.Controllers;

/// <summary>
/// Controller para gerenciamento de metas de vendas
/// </summary>
[ApiController]
[Route("api/sales-goals")]
[Authorize]
public class SalesGoalsController : ControllerBase
{
    private readonly ISalesGoalService _salesGoalService;
    private readonly ILogger<SalesGoalsController> _logger;

    public SalesGoalsController(ISalesGoalService salesGoalService, ILogger<SalesGoalsController> logger)
    {
        _salesGoalService = salesGoalService;
        _logger = logger;
    }

    /// <summary>
    /// Obtém todas as metas de um usuário
    /// </summary>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(List<SalesGoalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<SalesGoalDto>>> GetUserGoals(int userId)
    {
        try
        {
            var goals = await _salesGoalService.GetByUserIdAsync(userId);
            return Ok(goals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting goals for user {UserId}", userId);
            return StatusCode(500, "Erro ao buscar metas");
        }
    }

    /// <summary>
    /// Obtém meta de um usuário para um período específico
    /// </summary>
    [HttpGet("user/{userId}/period")]
    [ProducesResponseType(typeof(SalesGoalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SalesGoalDto>> GetUserGoalForPeriod(
        int userId,
        [FromQuery] int year,
        [FromQuery] int month)
    {
        try
        {
            var goal = await _salesGoalService.GetByUserAndPeriodAsync(userId, year, month);
            return goal == null ? NotFound() : Ok(goal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting goal for user {UserId} in period {Month}/{Year}", userId, month, year);
            return StatusCode(500, "Erro ao buscar meta");
        }
    }

    /// <summary>
    /// Obtém todas as metas do tenant para um período específico
    /// </summary>
    [HttpGet("tenant/{tenantId}/period")]
    [ProducesResponseType(typeof(List<SalesGoalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<SalesGoalDto>>> GetTenantGoalsForPeriod(
        int tenantId,
        [FromQuery] int year,
        [FromQuery] int month)
    {
        try
        {
            var goals = await _salesGoalService.GetByTenantAndPeriodAsync(tenantId, year, month);
            return Ok(goals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting goals for tenant {TenantId} in period {Month}/{Year}", tenantId, month, year);
            return StatusCode(500, "Erro ao buscar metas");
        }
    }

    /// <summary>
    /// Cria uma nova meta de vendas
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SalesGoalDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SalesGoalDto>> CreateGoal([FromBody] CreateSalesGoalDto dto)
    {
        try
        {
            var goal = await _salesGoalService.CreateAsync(dto);
            return CreatedAtAction(
                nameof(GetUserGoalForPeriod),
                new { userId = goal.UserId, year = goal.Year, month = goal.Month },
                goal);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sales goal");
            return StatusCode(500, "Erro ao criar meta");
        }
    }

    /// <summary>
    /// Atualiza uma meta de vendas existente
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(SalesGoalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SalesGoalDto>> UpdateGoal(int id, [FromBody] UpdateSalesGoalDto dto)
    {
        try
        {
            var goal = await _salesGoalService.UpdateAsync(id, dto);
            return Ok(goal);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Meta {id} não encontrada");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating sales goal {GoalId}", id);
            return StatusCode(500, "Erro ao atualizar meta");
        }
    }

    /// <summary>
    /// Exclui uma meta de vendas
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteGoal(int id)
    {
        try
        {
            await _salesGoalService.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting sales goal {GoalId}", id);
            return StatusCode(500, "Erro ao excluir meta");
        }
    }
}
