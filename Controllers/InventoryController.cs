using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using erp.Services.Inventory;
using erp.DTOs.Inventory;

namespace erp.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IStockCountService _stockCountService;
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(
        IStockCountService stockCountService,
        IInventoryService inventoryService,
        ILogger<InventoryController> logger)
    {
        _stockCountService = stockCountService;
        _inventoryService = inventoryService;
        _logger = logger;
    }

    #region Stock Counts

    /// <summary>
    /// Lista contagens de estoque com paginação e filtros
    /// </summary>
    [HttpGet("counts")]
    public async Task<ActionResult> GetStockCounts(
        [FromQuery] string? status = null,
        [FromQuery] int? warehouseId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var (counts, totalCount) = await _stockCountService.GetCountsAsync(
                status, warehouseId, page, pageSize);
            
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            
            return Ok(new
            {
                items = counts,
                totalItems = totalCount,
                page,
                pageSize,
                totalPages,
                hasNextPage = page < totalPages,
                hasPreviousPage = page > 1
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar contagens de estoque");
            return StatusCode(500, new { message = "Erro ao listar contagens", error = ex.Message });
        }
    }

    /// <summary>
    /// Cria uma nova contagem de estoque
    /// </summary>
    [HttpPost("counts")]
    public async Task<ActionResult<StockCountDto>> CreateStockCount([FromBody] CreateStockCountDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Usuário não autenticado" });
            }

            var count = await _stockCountService.CreateCountAsync(createDto, userId);

            return CreatedAtAction(nameof(GetStockCountById), new { id = count.Id }, count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar contagem de estoque");
            return StatusCode(500, new { message = "Erro ao criar contagem", error = ex.Message });
        }
    }

    /// <summary>
    /// Busca contagem de estoque por ID
    /// </summary>
    [HttpGet("stock-counts/{id:int}")]
    public async Task<ActionResult<StockCountDto>> GetStockCountById(int id)
    {
        try
        {
            var count = await _stockCountService.GetCountByIdAsync(id);
            
            if (count == null)
            {
                return NotFound(new { message = $"Contagem com ID {id} não encontrada" });
            }

            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar contagem {CountId}", id);
            return StatusCode(500, new { message = "Erro ao buscar contagem", error = ex.Message });
        }
    }

    /// <summary>
    /// Lista contagens ativas
    /// </summary>
    [HttpGet("stock-counts/active")]
    public async Task<ActionResult> GetActiveCounts()
    {
        try
        {
            var counts = await _stockCountService.GetActiveCountsAsync();
            return Ok(counts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar contagens ativas");
            return StatusCode(500, new { message = "Erro ao listar contagens", error = ex.Message });
        }
    }

    /// <summary>
    /// Adiciona um item à contagem de estoque
    /// </summary>
    [HttpPost("stock-counts/{countId:int}/items")]
    public async Task<ActionResult<StockCountDto>> AddItemToCount(
        int countId,
        [FromBody] AddStockCountItemDto itemDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Define o ID da contagem no DTO
            itemDto.StockCountId = countId;

            // Valida se o produto existe
            var product = await _inventoryService.GetProductByIdAsync(itemDto.ProductId);
            if (product == null)
            {
                return NotFound(new { message = $"Produto com ID {itemDto.ProductId} não encontrado" });
            }

            var count = await _stockCountService.AddItemToCountAsync(itemDto);

            return Ok(count);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Erro de validação ao adicionar item à contagem {CountId}", countId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar item à contagem {CountId}", countId);
            return StatusCode(500, new { message = "Erro ao adicionar item", error = ex.Message });
        }
    }

    /// <summary>
    /// Aprova a contagem e aplica os ajustes no estoque
    /// </summary>
    [HttpPost("stock-counts/{id:int}/approve")]
    public async Task<ActionResult<StockCountDto>> ApproveCount(
        int id,
        [FromBody] ApproveStockCountDto approveDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Usuário não autenticado" });
            }

            // Define o ID da contagem no DTO
            approveDto.StockCountId = id;
            
            var count = await _stockCountService.ApproveCountAsync(approveDto, userId);

            return Ok(count);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Erro de validação ao aprovar contagem {CountId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao aprovar contagem {CountId}", id);
            return StatusCode(500, new { message = "Erro ao aprovar contagem", error = ex.Message });
        }
    }

    /// <summary>
    /// Cancela uma contagem de estoque
    /// </summary>
    [HttpPost("stock-counts/{id:int}/cancel")]
    public async Task<ActionResult> CancelCount(int id)
    {
        try
        {
            var success = await _stockCountService.CancelCountAsync(id);
            
            if (!success)
            {
                return NotFound(new { message = $"Contagem com ID {id} não encontrada" });
            }

            return Ok(new { message = "Contagem cancelada com sucesso" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Erro de validação ao cancelar contagem {CountId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao cancelar contagem {CountId}", id);
            return StatusCode(500, new { message = "Erro ao cancelar contagem", error = ex.Message });
        }
    }

    #endregion

    #region Stock Movements

    /// <summary>
    /// Lista movimentações de estoque com paginação e filtros
    /// </summary>
    [HttpGet("movements")]
    public async Task<ActionResult> GetStockMovements(
        [FromQuery] int? productId = null,
        [FromQuery] string? movementType = null,
        [FromQuery] int? warehouseId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var (movements, totalCount) = await _inventoryService.GetStockMovementsAsync(
                productId, movementType, warehouseId, startDate, endDate, page, pageSize);
            
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            
            return Ok(new
            {
                items = movements,
                totalItems = totalCount,
                page,
                pageSize,
                totalPages,
                hasNextPage = page < totalPages,
                hasPreviousPage = page > 1
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar movimentações de estoque");
            return StatusCode(500, new { message = "Erro ao listar movimentações", error = ex.Message });
        }
    }

    /// <summary>
    /// Cria uma nova movimentação de estoque
    /// </summary>
    [HttpPost("movements")]
    public async Task<ActionResult<StockMovementDto>> CreateStockMovement([FromBody] CreateStockMovementDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Usuário não autenticado" });
            }

            var movement = await _inventoryService.CreateStockMovementAsync(createDto, userId);
            
            return CreatedAtAction(nameof(GetStockMovementById), new { id = movement.Id }, movement);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Erro de validação ao criar movimentação");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar movimentação de estoque");
            return StatusCode(500, new { message = "Erro ao criar movimentação", error = ex.Message });
        }
    }

    /// <summary>
    /// Busca movimentação por ID
    /// </summary>
    [HttpGet("movements/{id:int}")]
    public async Task<ActionResult<StockMovementDto>> GetStockMovementById(int id)
    {
        try
        {
            var movement = await _inventoryService.GetStockMovementByIdAsync(id);
            
            if (movement == null)
            {
                return NotFound(new { message = $"Movimentação com ID {id} não encontrada" });
            }

            return Ok(movement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar movimentação {MovementId}", id);
            return StatusCode(500, new { message = "Erro ao buscar movimentação", error = ex.Message });
        }
    }

    #endregion

    #region Warehouses

    /// <summary>
    /// Lista todos os armazéns/depósitos
    /// </summary>
    [HttpGet("warehouses")]
    public async Task<ActionResult> GetWarehouses(
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        try
        {
            var (warehouses, totalCount) = await _inventoryService.GetWarehousesAsync(
                isActive, page, pageSize);
            
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            
            return Ok(new
            {
                items = warehouses,
                totalItems = totalCount,
                page,
                pageSize,
                totalPages,
                hasNextPage = page < totalPages,
                hasPreviousPage = page > 1
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar armazéns");
            return StatusCode(500, new { message = "Erro ao listar armazéns", error = ex.Message });
        }
    }

    #endregion

    #region Alerts & Reports

    /// <summary>
    /// Obtém todos os alertas de estoque consolidados
    /// </summary>
    [HttpGet("alerts")]
    public async Task<ActionResult> GetAlerts()
    {
        try
        {
            var alerts = await _inventoryService.GetStockAlertsAsync();
            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter alertas de estoque");
            return StatusCode(500, new { message = "Erro ao obter alertas", error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém produtos com estoque baixo
    /// </summary>
    [HttpGet("alerts/low-stock")]
    public async Task<ActionResult> GetLowStockProducts()
    {
        try
        {
            var products = await _inventoryService.GetLowStockProductsAsync();
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter produtos com estoque baixo");
            return StatusCode(500, new { message = "Erro ao obter produtos", error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém produtos com excesso de estoque
    /// </summary>
    [HttpGet("alerts/overstock")]
    public async Task<ActionResult> GetOverstockProducts()
    {
        try
        {
            var products = await _inventoryService.GetOverstockProductsAsync();
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter produtos com excesso de estoque");
            return StatusCode(500, new { message = "Erro ao obter produtos", error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém produtos inativos (sem movimentação nos últimos X dias)
    /// </summary>
    [HttpGet("alerts/inactive")]
    public async Task<ActionResult> GetInactiveProducts([FromQuery] int days = 90)
    {
        try
        {
            if (days < 1) days = 90;
            
            var products = await _inventoryService.GetInactiveProductsAsync(days);
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter produtos inativos");
            return StatusCode(500, new { message = "Erro ao obter produtos", error = ex.Message });
        }
    }

    #endregion
}
