using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using erp.DTOs.Financial;
using erp.Services.Financial;
using erp.Models.Financial;
using System.Security.Claims;

namespace erp.Controllers;

[ApiController]
[Route("api/categorias-financeiras")]
[Authorize]
public class FinancialCategoriesController : ControllerBase
{
    private readonly IFinancialCategoryService _categoryService;
    private readonly ILogger<FinancialCategoriesController> _logger;

    public FinancialCategoriesController(IFinancialCategoryService categoryService, ILogger<FinancialCategoriesController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    /// <summary>
    /// Obter todas as categorias financeiras
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<FinancialCategoryDto>>> GetAll([FromQuery] bool activeOnly = false)
    {
        try
        {
            var categories = await _categoryService.GetAllAsync(activeOnly);
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting financial categories");
            return StatusCode(500, "Erro ao buscar categorias financeiras");
        }
    }

    /// <summary>
    /// Obter categoria por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<FinancialCategoryDto>> GetById(int id)
    {
        try
        {
            var category = await _categoryService.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound($"Categoria com ID {id} não encontrada");
            }
            return Ok(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category {CategoryId}", id);
            return StatusCode(500, "Erro ao buscar categoria");
        }
    }

    /// <summary>
    /// Obter categorias por tipo (Receita ou Despesa)
    /// </summary>
    [HttpGet("by-type/{type}")]
    public async Task<ActionResult<List<FinancialCategoryDto>>> GetByType(CategoryType type, [FromQuery] bool activeOnly = false)
    {
        try
        {
            var categories = await _categoryService.GetByTypeAsync(type, activeOnly);
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories by type {Type}", type);
            return StatusCode(500, "Erro ao buscar categorias");
        }
    }

    /// <summary>
    /// Obter categorias raiz (sem pai)
    /// </summary>
    [HttpGet("root")]
    public async Task<ActionResult<List<FinancialCategoryDto>>> GetRootCategories()
    {
        try
        {
            var categories = await _categoryService.GetRootCategoriesAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting root categories");
            return StatusCode(500, "Erro ao buscar categorias raiz");
        }
    }

    /// <summary>
    /// Obter subcategorias de uma categoria pai
    /// </summary>
    [HttpGet("{parentId}/subcategories")]
    public async Task<ActionResult<List<FinancialCategoryDto>>> GetSubCategories(int parentId)
    {
        try
        {
            var categories = await _categoryService.GetSubCategoriesAsync(parentId);
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subcategories for parent {ParentId}", parentId);
            return StatusCode(500, "Erro ao buscar subcategorias");
        }
    }

    /// <summary>
    /// Criar nova categoria
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<FinancialCategoryDto>> Create([FromBody] CreateFinancialCategoryDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int currentUserId))
            {
                return Unauthorized();
            }

            var category = await _categoryService.CreateAsync(dto, currentUserId);
            return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            return StatusCode(500, "Erro ao criar categoria");
        }
    }

    /// <summary>
    /// Atualizar categoria existente
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<FinancialCategoryDto>> Update(int id, [FromBody] UpdateFinancialCategoryDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int currentUserId))
            {
                return Unauthorized();
            }

            var category = await _categoryService.UpdateAsync(id, dto, currentUserId);
            if (category == null)
            {
                return NotFound($"Categoria com ID {id} não encontrada");
            }
            return Ok(category);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category {CategoryId}", id);
            return StatusCode(500, "Erro ao atualizar categoria");
        }
    }

    /// <summary>
    /// Excluir categoria
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            await _categoryService.DeleteAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category {CategoryId}", id);
            return StatusCode(500, "Erro ao excluir categoria");
        }
    }
}
