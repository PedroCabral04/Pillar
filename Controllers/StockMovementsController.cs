using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using erp.Services.Inventory;
using erp.DTOs.Inventory;

namespace erp.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class StockMovementsController : ControllerBase
{
    private readonly IStockMovementService _stockMovementService;
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<StockMovementsController> _logger;

    public StockMovementsController(
        IStockMovementService stockMovementService,
        IInventoryService inventoryService,
        ILogger<StockMovementsController> logger)
    {
        _stockMovementService = stockMovementService;
        _inventoryService = inventoryService;
        _logger = logger;
    }

    /// <summary>
    /// Cria uma movimentação genérica de estoque
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<StockMovementDto>> CreateMovement([FromBody] CreateStockMovementDto createDto)
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

            // Valida se o produto existe
            var product = await _inventoryService.GetProductByIdAsync(createDto.ProductId);
            if (product == null)
            {
                return NotFound(new { message = $"Produto com ID {createDto.ProductId} não encontrado" });
            }

            var movement = await _stockMovementService.CreateMovementAsync(createDto, userId);

            return CreatedAtAction(nameof(GetMovementById), new { id = movement.Id }, movement);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Erro de validação ao criar movimentação");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar movimentação");
            return StatusCode(500, new { message = "Erro ao criar movimentação", error = ex.Message });
        }
    }

    /// <summary>
    /// Cria uma movimentação de entrada de estoque
    /// </summary>
    [HttpPost("entry")]
    public async Task<ActionResult<StockMovementDto>> CreateEntry([FromBody] CreateStockMovementDto createDto)
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

            // Valida se o produto existe
            var product = await _inventoryService.GetProductByIdAsync(createDto.ProductId);
            if (product == null)
            {
                return NotFound(new { message = $"Produto com ID {createDto.ProductId} não encontrado" });
            }

            var movement = await _stockMovementService.CreateEntryAsync(
                createDto.ProductId,
                createDto.Quantity,
                createDto.UnitCost,
                createDto.DocumentNumber,
                createDto.Notes,
                userId,
                createDto.WarehouseId
            );

            return CreatedAtAction(nameof(GetMovementById), new { id = movement.Id }, movement);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Erro de validação ao criar entrada de estoque");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar entrada de estoque");
            return StatusCode(500, new { message = "Erro ao criar entrada", error = ex.Message });
        }
    }

    /// <summary>
    /// Cria uma movimentação de saída de estoque
    /// </summary>
    [HttpPost("exit")]
    public async Task<ActionResult<StockMovementDto>> CreateExit([FromBody] CreateStockMovementDto createDto)
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

            // Valida se o produto existe
            var product = await _inventoryService.GetProductByIdAsync(createDto.ProductId);
            if (product == null)
            {
                return NotFound(new { message = $"Produto com ID {createDto.ProductId} não encontrado" });
            }

            var movement = await _stockMovementService.CreateExitAsync(
                createDto.ProductId,
                createDto.Quantity,
                createDto.DocumentNumber,
                createDto.Notes,
                userId,
                createDto.WarehouseId
            );

            return CreatedAtAction(nameof(GetMovementById), new { id = movement.Id }, movement);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Erro de validação ao criar saída de estoque: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar saída de estoque");
            return StatusCode(500, new { message = "Erro ao criar saída", error = ex.Message });
        }
    }

    /// <summary>
    /// Cria uma movimentação de ajuste de estoque
    /// </summary>
    [HttpPost("adjustment")]
    public async Task<ActionResult<StockMovementDto>> CreateAdjustment([FromBody] CreateStockAdjustmentDto adjustmentDto)
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

            // Valida se o produto existe
            var product = await _inventoryService.GetProductByIdAsync(adjustmentDto.ProductId);
            if (product == null)
            {
                return NotFound(new { message = $"Produto com ID {adjustmentDto.ProductId} não encontrado" });
            }

            var movement = await _stockMovementService.CreateAdjustmentAsync(
                adjustmentDto.ProductId,
                adjustmentDto.NewStock,
                adjustmentDto.Reason,
                userId,
                adjustmentDto.WarehouseId
            );

            return CreatedAtAction(nameof(GetMovementById), new { id = movement.Id }, movement);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Erro de validação ao criar ajuste de estoque");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar ajuste de estoque");
            return StatusCode(500, new { message = "Erro ao criar ajuste", error = ex.Message });
        }
    }

    /// <summary>
    /// Busca movimentação por ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<StockMovementDto>> GetMovementById(int id)
    {
        try
        {
            var movement = await _stockMovementService.GetMovementByIdAsync(id);
            
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

    /// <summary>
    /// Obtém histórico de movimentações de um produto
    /// </summary>
    [HttpGet("product/{productId:int}")]
    public async Task<ActionResult> GetMovementsByProduct(
        int productId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            // Valida se o produto existe
            var product = await _inventoryService.GetProductByIdAsync(productId);
            if (product == null)
            {
                return NotFound(new { message = $"Produto com ID {productId} não encontrado" });
            }

            var movements = await _stockMovementService.GetMovementsByProductAsync(
                productId,
                startDate,
                endDate
            );

            return Ok(movements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter movimentações do produto {ProductId}", productId);
            return StatusCode(500, new { message = "Erro ao obter movimentações", error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém movimentações por período
    /// </summary>
    [HttpGet("date-range")]
    public async Task<ActionResult> GetMovementsByDateRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int? warehouseId = null)
    {
        try
        {
            if (startDate > endDate)
            {
                return BadRequest(new { message = "Data inicial deve ser anterior à data final" });
            }

            var movements = await _stockMovementService.GetMovementsByDateRangeAsync(
                startDate,
                endDate,
                warehouseId
            );

            return Ok(movements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter movimentações por período");
            return StatusCode(500, new { message = "Erro ao obter movimentações", error = ex.Message });
        }
    }
}
