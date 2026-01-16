using Microsoft.AspNetCore.Mvc;
using erp.DTOs.User;
using erp.Services.Administration;
using Microsoft.AspNetCore.Authorization;
using erp.Models.Audit;

namespace erp.Controllers;

[ApiController]
[Route("api/departamentos")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    /// <summary>
    /// Recupera todos os departamentos, incluindo departamento pai, informações do gestor e contagem de funcionários.
    /// </summary>
    /// <returns>200 OK com uma coleção de <see cref="DepartmentDto"/>.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DepartmentDto>>> GetAllDepartments()
    {
        var departments = await _departmentService.GetAllAsync();
        return Ok(departments);
    }

    /// <summary>
    /// Recupera um único departamento pelo seu identificador, incluindo departamento pai, gestor e funcionários relacionados.
    /// </summary>
    /// <param name="id">O identificador do departamento.</param>
    /// <returns>200 OK com <see cref="DepartmentDto"/>, ou 404 Not Found se o departamento não existir.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AuditRead("Department", DataSensitivity.Medium, Description = "Visualização de informações do departamento e funcionários")]
    public async Task<ActionResult<DepartmentDto>> GetDepartmentById(int id)
    {
        var department = await _departmentService.GetByIdAsync(id);

        if (department == null)
            return NotFound($"Departamento com ID {id} não encontrado.");

        return Ok(department);
    }

    /// <summary>
    /// Cria um novo departamento.
    /// </summary>
    /// <param name="createDto">Dados do departamento usados para criar o registro.</param>
    /// <returns>201 Created com o <see cref="DepartmentDto"/> criado, ou 400 Bad Request em caso de entrada inválida.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DepartmentDto>> CreateDepartment([FromBody] CreateDepartmentDto createDto)
    {
        var department = await _departmentService.CreateAsync(createDto);
        return CreatedAtAction(nameof(GetDepartmentById), new { id = department.Id }, department);
    }

    /// <summary>
    /// Atualiza um departamento existente com os valores fornecidos.
    /// </summary>
    /// <param name="id">O identificador do departamento a ser atualizado.</param>
    /// <param name="updateDto">O payload de atualização.</param>
    /// <returns>204 No Content em sucesso, ou 404 Not Found se o departamento não existir.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDepartment(int id, [FromBody] UpdateDepartmentDto updateDto)
    {
        await _departmentService.UpdateAsync(id, updateDto);
        return NoContent();
    }

    /// <summary>
    /// Exclui um departamento se ele não tiver funcionários ou subdepartamentos.
    /// </summary>
    /// <param name="id">O identificador do departamento a ser excluído.</param>
    /// <returns>204 No Content em sucesso, 400 Bad Request se existirem restrições de exclusão, ou 404 se não existir.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteDepartment(int id)
    {
        await _departmentService.DeleteAsync(id);
        return NoContent();
    }
}
