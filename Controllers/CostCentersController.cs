using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using erp.DTOs.Financial;
using erp.Services.Financial;
using System.Security.Claims;

namespace erp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CostCentersController : ControllerBase
{
    private readonly ICostCenterService _costCenterService;
    private readonly ILogger<CostCentersController> _logger;

    public CostCentersController(ICostCenterService costCenterService, ILogger<CostCentersController> logger)
    {
        _costCenterService = costCenterService;
        _logger = logger;
    }

    /// <summary>
    /// Obter todos os centros de custo
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<CostCenterDto>>> GetAll()
    {
        try
        {
            var costCenters = await _costCenterService.GetAllAsync();
            return Ok(costCenters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cost centers");
            return StatusCode(500, "Erro ao buscar centros de custo");
        }
    }

    /// <summary>
    /// Obter centro de custo por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CostCenterDto>> GetById(int id)
    {
        try
        {
            var costCenter = await _costCenterService.GetByIdAsync(id);
            if (costCenter == null)
            {
                return NotFound($"Centro de custo com ID {id} não encontrado");
            }
            return Ok(costCenter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cost center {CostCenterId}", id);
            return StatusCode(500, "Erro ao buscar centro de custo");
        }
    }

    /// <summary>
    /// Criar novo centro de custo
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CostCenterDto>> Create([FromBody] CreateCostCenterDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int currentUserId))
            {
                return Unauthorized();
            }

            var costCenter = await _costCenterService.CreateAsync(dto, currentUserId);
            return CreatedAtAction(nameof(GetById), new { id = costCenter.Id }, costCenter);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cost center");
            return StatusCode(500, "Erro ao criar centro de custo");
        }
    }

    /// <summary>
    /// Atualizar centro de custo existente
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<CostCenterDto>> Update(int id, [FromBody] UpdateCostCenterDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int currentUserId))
            {
                return Unauthorized();
            }

            var costCenter = await _costCenterService.UpdateAsync(id, dto, currentUserId);
            if (costCenter == null)
            {
                return NotFound($"Centro de custo com ID {id} não encontrado");
            }
            return Ok(costCenter);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cost center {CostCenterId}", id);
            return StatusCode(500, "Erro ao atualizar centro de custo");
        }
    }

    /// <summary>
    /// Excluir centro de custo
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            await _costCenterService.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting cost center {CostCenterId}", id);
            return StatusCode(500, "Erro ao excluir centro de custo");
        }
    }

    /// <summary>
    /// Alternar status ativo do centro de custo
    /// </summary>
    [HttpPatch("{id}/toggle-active")]
    public async Task<ActionResult<CostCenterDto>> ToggleActive(int id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int currentUserId))
            {
                return Unauthorized();
            }

            var costCenter = await _costCenterService.ToggleActiveAsync(id, currentUserId);
            if (costCenter == null)
            {
                return NotFound($"Centro de custo com ID {id} não encontrado");
            }
            return Ok(costCenter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling cost center active status {CostCenterId}", id);
            return StatusCode(500, "Erro ao alterar status do centro de custo");
        }
    }
}
