using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using erp.Services.Assets;
using erp.DTOs.Assets;
using erp.Models;
using System.Security.Claims;

namespace erp.Controllers;

/// <summary>
/// Controller para gerenciamento de ativos da empresa
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AssetsController : ControllerBase
{
    private readonly IAssetService _assetService;
    private readonly ILogger<AssetsController> _logger;

    public AssetsController(
        IAssetService assetService,
        ILogger<AssetsController> logger)
    {
        _assetService = assetService;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return int.Parse(userIdClaim!.Value);
    }

    // ============= Asset Management =============

    /// <summary>
    /// Lista todos os ativos
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<AssetDto>>> GetAllAssets()
    {
        try
        {
            var assets = await _assetService.GetAllAssetsAsync();
            return Ok(assets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar ativos");
            return StatusCode(500, new { message = "Erro ao buscar ativos", error = ex.Message });
        }
    }

    /// <summary>
    /// Busca ativo por ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetDto>> GetAssetById(int id)
    {
        try
        {
            var asset = await _assetService.GetAssetByIdAsync(id);
            if (asset == null)
            {
                return NotFound(new { message = "Ativo não encontrado" });
            }
            return Ok(asset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar ativo {AssetId}", id);
            return StatusCode(500, new { message = "Erro ao buscar ativo", error = ex.Message });
        }
    }

    /// <summary>
    /// Busca ativo por código
    /// </summary>
    [HttpGet("code/{assetCode}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetDto>> GetAssetByCode(string assetCode)
    {
        try
        {
            var asset = await _assetService.GetAssetByCodeAsync(assetCode);
            if (asset == null)
            {
                return NotFound(new { message = "Ativo não encontrado" });
            }
            return Ok(asset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar ativo com código {AssetCode}", assetCode);
            return StatusCode(500, new { message = "Erro ao buscar ativo", error = ex.Message });
        }
    }

    /// <summary>
    /// Lista ativos por categoria
    /// </summary>
    [HttpGet("category/{categoryId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AssetDto>>> GetAssetsByCategory(int categoryId)
    {
        try
        {
            var assets = await _assetService.GetAssetsByCategoryAsync(categoryId);
            return Ok(assets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar ativos da categoria {CategoryId}", categoryId);
            return StatusCode(500, new { message = "Erro ao buscar ativos", error = ex.Message });
        }
    }

    /// <summary>
    /// Lista ativos por status
    /// </summary>
    [HttpGet("status/{status}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AssetDto>>> GetAssetsByStatus(AssetStatus status)
    {
        try
        {
            var assets = await _assetService.GetAssetsByStatusAsync(status);
            return Ok(assets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar ativos com status {Status}", status);
            return StatusCode(500, new { message = "Erro ao buscar ativos", error = ex.Message });
        }
    }

    /// <summary>
    /// Cria novo ativo
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssetDto>> CreateAsset([FromBody] CreateAssetDto dto)
    {
        try
        {
            var asset = await _assetService.CreateAssetAsync(dto);
            return CreatedAtAction(nameof(GetAssetById), new { id = asset.Id }, asset);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar ativo");
            return StatusCode(500, new { message = "Erro ao criar ativo", error = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza ativo existente
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetDto>> UpdateAsset(int id, [FromBody] UpdateAssetDto dto)
    {
        try
        {
            var asset = await _assetService.UpdateAssetAsync(id, dto);
            return Ok(asset);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar ativo {AssetId}", id);
            return StatusCode(500, new { message = "Erro ao atualizar ativo", error = ex.Message });
        }
    }

    /// <summary>
    /// Exclui (desativa) ativo
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAsset(int id)
    {
        try
        {
            await _assetService.DeleteAssetAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao excluir ativo {AssetId}", id);
            return StatusCode(500, new { message = "Erro ao excluir ativo", error = ex.Message });
        }
    }

    // ============= Asset Assignment =============

    /// <summary>
    /// Busca atribuição por ID
    /// </summary>
    [HttpGet("assignments/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetAssignmentDto>> GetAssignmentById(int id)
    {
        try
        {
            var assignment = await _assetService.GetAssignmentByIdAsync(id);
            if (assignment == null)
            {
                return NotFound(new { message = "Atribuição não encontrada" });
            }
            return Ok(assignment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar atribuição {AssignmentId}", id);
            return StatusCode(500, new { message = "Erro ao buscar atribuição", error = ex.Message });
        }
    }

    /// <summary>
    /// Busca atribuição atual de um ativo
    /// </summary>
    [HttpGet("{assetId}/current-assignment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetAssignmentDto>> GetCurrentAssignment(int assetId)
    {
        try
        {
            var assignment = await _assetService.GetCurrentAssignmentForAssetAsync(assetId);
            if (assignment == null)
            {
                return NotFound(new { message = "Ativo não possui atribuição atual" });
            }
            return Ok(assignment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar atribuição atual do ativo {AssetId}", assetId);
            return StatusCode(500, new { message = "Erro ao buscar atribuição", error = ex.Message });
        }
    }

    /// <summary>
    /// Lista histórico de atribuições de um ativo
    /// </summary>
    [HttpGet("{assetId}/assignments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AssetAssignmentDto>>> GetAssignmentHistory(int assetId)
    {
        try
        {
            var assignments = await _assetService.GetAssignmentHistoryForAssetAsync(assetId);
            return Ok(assignments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar histórico de atribuições do ativo {AssetId}", assetId);
            return StatusCode(500, new { message = "Erro ao buscar histórico", error = ex.Message });
        }
    }

    /// <summary>
    /// Lista atribuições de um usuário
    /// </summary>
    [HttpGet("users/{userId}/assignments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AssetAssignmentDto>>> GetUserAssignments(int userId, [FromQuery] bool includeReturned = false)
    {
        try
        {
            var assignments = await _assetService.GetAssignmentsForUserAsync(userId, includeReturned);
            return Ok(assignments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar atribuições do usuário {UserId}", userId);
            return StatusCode(500, new { message = "Erro ao buscar atribuições", error = ex.Message });
        }
    }

    /// <summary>
    /// Atribui ativo a um funcionário
    /// </summary>
    [HttpPost("assignments")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssetAssignmentDto>> AssignAsset([FromBody] CreateAssetAssignmentDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var assignment = await _assetService.AssignAssetAsync(dto, userId);
            return CreatedAtAction(nameof(GetAssignmentById), new { id = assignment.Id }, assignment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atribuir ativo");
            return StatusCode(500, new { message = "Erro ao atribuir ativo", error = ex.Message });
        }
    }

    /// <summary>
    /// Registra devolução de ativo
    /// </summary>
    [HttpPost("assignments/{assignmentId}/return")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetAssignmentDto>> ReturnAsset(int assignmentId, [FromBody] ReturnAssetDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var assignment = await _assetService.ReturnAssetAsync(assignmentId, dto, userId);
            return Ok(assignment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar devolução da atribuição {AssignmentId}", assignmentId);
            return StatusCode(500, new { message = "Erro ao registrar devolução", error = ex.Message });
        }
    }

    // ============= Asset Maintenance =============

    /// <summary>
    /// Busca manutenção por ID
    /// </summary>
    [HttpGet("maintenances/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetMaintenanceDto>> GetMaintenanceById(int id)
    {
        try
        {
            var maintenance = await _assetService.GetMaintenanceByIdAsync(id);
            if (maintenance == null)
            {
                return NotFound(new { message = "Manutenção não encontrada" });
            }
            return Ok(maintenance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar manutenção {MaintenanceId}", id);
            return StatusCode(500, new { message = "Erro ao buscar manutenção", error = ex.Message });
        }
    }

    /// <summary>
    /// Lista histórico de manutenções de um ativo
    /// </summary>
    [HttpGet("{assetId}/maintenances")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AssetMaintenanceDto>>> GetMaintenanceHistory(int assetId)
    {
        try
        {
            var maintenances = await _assetService.GetMaintenanceHistoryForAssetAsync(assetId);
            return Ok(maintenances);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar histórico de manutenções do ativo {AssetId}", assetId);
            return StatusCode(500, new { message = "Erro ao buscar histórico", error = ex.Message });
        }
    }

    /// <summary>
    /// Lista manutenções agendadas
    /// </summary>
    [HttpGet("maintenances/scheduled")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AssetMaintenanceDto>>> GetScheduledMaintenances()
    {
        try
        {
            var maintenances = await _assetService.GetScheduledMaintenancesAsync();
            return Ok(maintenances);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar manutenções agendadas");
            return StatusCode(500, new { message = "Erro ao buscar manutenções", error = ex.Message });
        }
    }

    /// <summary>
    /// Lista manutenções atrasadas
    /// </summary>
    [HttpGet("maintenances/overdue")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AssetMaintenanceDto>>> GetOverdueMaintenances()
    {
        try
        {
            var maintenances = await _assetService.GetOverdueMaintenancesAsync();
            return Ok(maintenances);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar manutenções atrasadas");
            return StatusCode(500, new { message = "Erro ao buscar manutenções", error = ex.Message });
        }
    }

    /// <summary>
    /// Cria nova manutenção
    /// </summary>
    [HttpPost("maintenances")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssetMaintenanceDto>> CreateMaintenance([FromBody] CreateAssetMaintenanceDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var maintenance = await _assetService.CreateMaintenanceAsync(dto, userId);
            return CreatedAtAction(nameof(GetMaintenanceById), new { id = maintenance.Id }, maintenance);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar manutenção");
            return StatusCode(500, new { message = "Erro ao criar manutenção", error = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza manutenção existente
    /// </summary>
    [HttpPut("maintenances/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetMaintenanceDto>> UpdateMaintenance(int id, [FromBody] UpdateAssetMaintenanceDto dto)
    {
        try
        {
            var maintenance = await _assetService.UpdateMaintenanceAsync(id, dto);
            return Ok(maintenance);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar manutenção {MaintenanceId}", id);
            return StatusCode(500, new { message = "Erro ao atualizar manutenção", error = ex.Message });
        }
    }

    /// <summary>
    /// Marca manutenção como concluída
    /// </summary>
    [HttpPost("maintenances/{id}/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetMaintenanceDto>> CompleteMaintenance(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var maintenance = await _assetService.CompleteMaintenanceAsync(id, userId);
            return Ok(maintenance);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao concluir manutenção {MaintenanceId}", id);
            return StatusCode(500, new { message = "Erro ao concluir manutenção", error = ex.Message });
        }
    }

    /// <summary>
    /// Exclui manutenção
    /// </summary>
    [HttpDelete("maintenances/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteMaintenance(int id)
    {
        try
        {
            await _assetService.DeleteMaintenanceAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao excluir manutenção {MaintenanceId}", id);
            return StatusCode(500, new { message = "Erro ao excluir manutenção", error = ex.Message });
        }
    }

    // ============= Categories =============

    /// <summary>
    /// Lista todas as categorias de ativos
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AssetCategoryDto>>> GetAllCategories()
    {
        try
        {
            var categories = await _assetService.GetAllCategoriesAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar categorias");
            return StatusCode(500, new { message = "Erro ao buscar categorias", error = ex.Message });
        }
    }

    /// <summary>
    /// Busca categoria por ID
    /// </summary>
    [HttpGet("categories/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetCategoryDto>> GetCategoryById(int id)
    {
        try
        {
            var category = await _assetService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound(new { message = "Categoria não encontrada" });
            }
            return Ok(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar categoria {CategoryId}", id);
            return StatusCode(500, new { message = "Erro ao buscar categoria", error = ex.Message });
        }
    }

    /// <summary>
    /// Cria nova categoria
    /// </summary>
    [HttpPost("categories")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssetCategoryDto>> CreateCategory([FromBody] CreateAssetCategoryDto dto)
    {
        try
        {
            var category = await _assetService.CreateCategoryAsync(dto);
            return CreatedAtAction(nameof(GetCategoryById), new { id = category.Id }, category);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar categoria");
            return StatusCode(500, new { message = "Erro ao criar categoria", error = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza categoria existente
    /// </summary>
    [HttpPut("categories/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetCategoryDto>> UpdateCategory(int id, [FromBody] UpdateAssetCategoryDto dto)
    {
        try
        {
            var category = await _assetService.UpdateCategoryAsync(id, dto);
            return Ok(category);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar categoria {CategoryId}", id);
            return StatusCode(500, new { message = "Erro ao atualizar categoria", error = ex.Message });
        }
    }

    /// <summary>
    /// Exclui (desativa) categoria
    /// </summary>
    [HttpDelete("categories/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteCategory(int id)
    {
        try
        {
            await _assetService.DeleteCategoryAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao excluir categoria {CategoryId}", id);
            return StatusCode(500, new { message = "Erro ao excluir categoria", error = ex.Message });
        }
    }

    // ============= Statistics =============

    /// <summary>
    /// Retorna estatísticas gerais dos ativos
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<AssetStatisticsDto>> GetStatistics()
    {
        try
        {
            var statistics = await _assetService.GetAssetStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar estatísticas");
            return StatusCode(500, new { message = "Erro ao buscar estatísticas", error = ex.Message });
        }
    }

    // ============= Document Management =============

    /// <summary>
    /// Lista todos os documentos de um ativo
    /// </summary>
    [HttpGet("{id}/documents")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<AssetDocumentDto>>> GetAssetDocuments(int id)
    {
        try
        {
            var documents = await _assetService.GetDocumentsByAssetIdAsync(id);
            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar documentos do ativo {AssetId}", id);
            return StatusCode(500, new { message = "Erro ao buscar documentos", error = ex.Message });
        }
    }

    /// <summary>
    /// Lista documentos de um ativo por tipo
    /// </summary>
    [HttpGet("{id}/documents/type/{type}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AssetDocumentDto>>> GetAssetDocumentsByType(int id, AssetDocumentType type)
    {
        try
        {
            var documents = await _assetService.GetDocumentsByTypeAsync(id, type);
            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar documentos do tipo {Type} do ativo {AssetId}", type, id);
            return StatusCode(500, new { message = "Erro ao buscar documentos", error = ex.Message });
        }
    }

    /// <summary>
    /// Busca documento por ID
    /// </summary>
    [HttpGet("documents/{docId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetDocumentDto>> GetDocumentById(int docId)
    {
        try
        {
            var document = await _assetService.GetDocumentByIdAsync(docId);
            if (document == null)
            {
                return NotFound(new { message = "Documento não encontrado" });
            }
            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar documento {DocumentId}", docId);
            return StatusCode(500, new { message = "Erro ao buscar documento", error = ex.Message });
        }
    }

    /// <summary>
    /// Faz upload de um documento para um ativo
    /// </summary>
    [HttpPost("{id}/documents")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetDocumentDto>> UploadDocument(
        int id,
        [FromForm] UploadAssetDocumentFormDto formData)
    {
        try
        {
            if (formData.File == null || formData.File.Length == 0)
            {
                return BadRequest(new { message = "Arquivo não fornecido ou vazio" });
            }

            // Validação de tamanho (50MB max)
            if (formData.File.Length > 50 * 1024 * 1024)
            {
                return BadRequest(new { message = "Arquivo muito grande. Tamanho máximo: 50MB" });
            }

            var dto = new CreateAssetDocumentDto
            {
                AssetId = id,
                Type = formData.Type,
                Description = formData.Description,
                DocumentNumber = formData.DocumentNumber,
                DocumentDate = formData.DocumentDate,
                ExpiryDate = formData.ExpiryDate
            };

            using var stream = formData.File.OpenReadStream();
            var userId = GetCurrentUserId();
            var document = await _assetService.CreateDocumentAsync(dto, stream, userId);
            
            return CreatedAtAction(
                nameof(GetDocumentById),
                new { docId = document.Id },
                document);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer upload de documento para ativo {AssetId}", id);
            return StatusCode(500, new { message = "Erro ao fazer upload de documento", error = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza metadados de um documento
    /// </summary>
    [HttpPut("documents/{docId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetDocumentDto>> UpdateDocument(int docId, [FromBody] UpdateAssetDocumentDto dto)
    {
        try
        {
            var document = await _assetService.UpdateDocumentAsync(docId, dto);
            return Ok(document);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar documento {DocumentId}", docId);
            return StatusCode(500, new { message = "Erro ao atualizar documento", error = ex.Message });
        }
    }

    /// <summary>
    /// Faz download de um documento
    /// </summary>
    [HttpGet("{id}/documents/{docId}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DownloadDocument(int id, int docId)
    {
        try
        {
            var document = await _assetService.GetDocumentByIdAsync(docId);
            if (document == null)
            {
                return NotFound(new { message = "Documento não encontrado" });
            }

            if (document.AssetId != id)
            {
                return BadRequest(new { message = "Documento não pertence a este ativo" });
            }

            var stream = await _assetService.DownloadDocumentAsync(docId);
            return File(stream, document.ContentType, document.OriginalFileName);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Arquivo não encontrado no servidor" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer download do documento {DocumentId}", docId);
            return StatusCode(500, new { message = "Erro ao fazer download do documento", error = ex.Message });
        }
    }

    /// <summary>
    /// Exclui um documento
    /// </summary>
    [HttpDelete("documents/{docId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteDocument(int docId)
    {
        try
        {
            await _assetService.DeleteDocumentAsync(docId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao excluir documento {DocumentId}", docId);
            return StatusCode(500, new { message = "Erro ao excluir documento", error = ex.Message });
        }
    }

    // ============= Transfer Management =============

    /// <summary>
    /// Lista histórico de transferências de um ativo
    /// </summary>
    [HttpGet("{id}/transfers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AssetTransferDto>>> GetAssetTransfers(int id)
    {
        try
        {
            var transfers = await _assetService.GetTransferHistoryForAssetAsync(id);
            return Ok(transfers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar transferências do ativo {AssetId}", id);
            return StatusCode(500, new { message = "Erro ao buscar transferências", error = ex.Message });
        }
    }

    /// <summary>
    /// Lista todas as transferências pendentes
    /// </summary>
    [HttpGet("transfers/pending")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AssetTransferDto>>> GetPendingTransfers()
    {
        try
        {
            var transfers = await _assetService.GetPendingTransfersAsync();
            return Ok(transfers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar transferências pendentes");
            return StatusCode(500, new { message = "Erro ao buscar transferências pendentes", error = ex.Message });
        }
    }

    /// <summary>
    /// Lista transferências por status
    /// </summary>
    [HttpGet("transfers/status/{status}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AssetTransferDto>>> GetTransfersByStatus(TransferStatus status)
    {
        try
        {
            var transfers = await _assetService.GetTransfersByStatusAsync(status);
            return Ok(transfers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar transferências com status {Status}", status);
            return StatusCode(500, new { message = "Erro ao buscar transferências", error = ex.Message });
        }
    }

    /// <summary>
    /// Busca transferência por ID
    /// </summary>
    [HttpGet("transfers/{transferId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetTransferDto>> GetTransferById(int transferId)
    {
        try
        {
            var transfer = await _assetService.GetTransferByIdAsync(transferId);
            if (transfer == null)
            {
                return NotFound(new { message = "Transferência não encontrada" });
            }
            return Ok(transfer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar transferência {TransferId}", transferId);
            return StatusCode(500, new { message = "Erro ao buscar transferência", error = ex.Message });
        }
    }

    /// <summary>
    /// Cria uma nova solicitação de transferência
    /// </summary>
    [HttpPost("transfers")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssetTransferDto>> CreateTransfer([FromBody] CreateAssetTransferDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var transfer = await _assetService.CreateTransferAsync(dto, userId);
            
            return CreatedAtAction(
                nameof(GetTransferById),
                new { transferId = transfer.Id },
                transfer);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar transferência");
            return StatusCode(500, new { message = "Erro ao criar transferência", error = ex.Message });
        }
    }

    /// <summary>
    /// Aprova uma transferência pendente
    /// </summary>
    [HttpPut("transfers/{transferId}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetTransferDto>> ApproveTransfer(int transferId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var transfer = await _assetService.ApproveTransferAsync(transferId, userId);
            return Ok(transfer);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao aprovar transferência {TransferId}", transferId);
            return StatusCode(500, new { message = "Erro ao aprovar transferência", error = ex.Message });
        }
    }

    /// <summary>
    /// Rejeita uma transferência pendente
    /// </summary>
    [HttpPut("transfers/{transferId}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetTransferDto>> RejectTransfer(int transferId, [FromBody] string reason)
    {
        try
        {
            var userId = GetCurrentUserId();
            var transfer = await _assetService.RejectTransferAsync(transferId, userId, reason);
            return Ok(transfer);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao rejeitar transferência {TransferId}", transferId);
            return StatusCode(500, new { message = "Erro ao rejeitar transferência", error = ex.Message });
        }
    }

    /// <summary>
    /// Completa uma transferência aprovada
    /// </summary>
    [HttpPut("transfers/{transferId}/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetTransferDto>> CompleteTransfer(int transferId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var transfer = await _assetService.CompleteTransferAsync(transferId, userId);
            return Ok(transfer);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao completar transferência {TransferId}", transferId);
            return StatusCode(500, new { message = "Erro ao completar transferência", error = ex.Message });
        }
    }

    /// <summary>
    /// Cancela uma transferência
    /// </summary>
    [HttpPut("transfers/{transferId}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetTransferDto>> CancelTransfer(int transferId, [FromBody] string reason)
    {
        try
        {
            var userId = GetCurrentUserId();
            var transfer = await _assetService.CancelTransferAsync(transferId, userId, reason);
            return Ok(transfer);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao cancelar transferência {TransferId}", transferId);
            return StatusCode(500, new { message = "Erro ao cancelar transferência", error = ex.Message });
        }
    }

    // ============= QR Code Generation =============

    /// <summary>
    /// Gera QR code para um ativo (retorna imagem PNG)
    /// </summary>
    [HttpGet("{id}/qrcode")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GenerateQRCode(int id)
    {
        try
        {
            var qrCodeBytes = await _assetService.GenerateAssetQRCodeAsync(id);
            return File(qrCodeBytes, "image/png");
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar QR code para ativo {AssetId}", id);
            return StatusCode(500, new { message = "Erro ao gerar QR code", error = ex.Message });
        }
    }

    /// <summary>
    /// Gera QR code para um ativo (retorna Base64 para exibição em HTML)
    /// </summary>
    [HttpGet("{id}/qrcode/base64")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> GenerateQRCodeBase64(int id)
    {
        try
        {
            var qrCodeBase64 = await _assetService.GenerateAssetQRCodeBase64Async(id);
            return Ok(new { qrCode = qrCodeBase64, mimeType = "image/png" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar QR code Base64 para ativo {AssetId}", id);
            return StatusCode(500, new { message = "Erro ao gerar QR code", error = ex.Message });
        }
    }
}
