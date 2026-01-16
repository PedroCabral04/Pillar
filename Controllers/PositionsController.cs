using Microsoft.AspNetCore.Mvc;
using erp.DTOs.User;
using erp.Services.Administration;
using Microsoft.AspNetCore.Authorization;
using erp.Models.Audit;

namespace erp.Controllers;

[ApiController]
[Route("api/cargos")]
[Authorize]
public class PositionsController : ControllerBase
{
    private readonly IPositionService _positionService;

    public PositionsController(IPositionService positionService)
    {
        _positionService = positionService;
    }

    /// <summary>
    /// Retorna a lista completa de cargos com informações relevantes.
    /// </summary>
    /// <returns>Lista de <see cref="PositionDto"/> com os cargos encontrados.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PositionDto>>> GetAllPositions()
    {
        var positions = await _positionService.GetAllAsync();
        return Ok(positions);
    }

    /// <summary>
    /// Recupera um cargo por seu identificador.
    /// </summary>
    /// <param name="id">Identificador do cargo.</param>
    /// <returns>Um <see cref="PositionDto"/> representando o cargo ou 404 se não encontrado.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AuditRead("Position", DataSensitivity.High, Description = "Visualização de cargo e faixas salariais")]
    public async Task<ActionResult<PositionDto>> GetPositionById(int id)
    {
        var position = await _positionService.GetByIdAsync(id);

        if (position == null)
            return NotFound($"Cargo com ID {id} não encontrado.");

        return Ok(position);
    }

    /// <summary>
    /// Cria um novo cargo no sistema.
    /// </summary>
    /// <param name="createDto">Dados necessários para criar o cargo.</param>
    /// <returns>Um <see cref="PositionDto"/> com o cargo criado (HTTP 201).</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PositionDto>> CreatePosition([FromBody] CreatePositionDto createDto)
    {
        var position = await _positionService.CreateAsync(createDto);
        return CreatedAtAction(nameof(GetPositionById), new { id = position.Id }, position);
    }

    /// <summary>
    /// Atualiza um cargo existente.
    /// </summary>
    /// <param name="id">Identificador do cargo a ser atualizado.</param>
    /// <param name="updateDto">Dados atualizados do cargo.</param>
    /// <returns>HTTP 204 em caso de sucesso ou 404 se não encontrado.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePosition(int id, [FromBody] UpdatePositionDto updateDto)
    {
        await _positionService.UpdateAsync(id, updateDto);
        return NoContent();
    }

    /// <summary>
    /// Remove um cargo do sistema se não houver funcionários atribuídos.
    /// </summary>
    /// <param name="id">Identificador do cargo a ser removido.</param>
    /// <returns>HTTP 204 se removido, 404 se não encontrado ou 400 se houver funcionários vinculados.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeletePosition(int id)
    {
        await _positionService.DeleteAsync(id);
        return NoContent();
    }
}
