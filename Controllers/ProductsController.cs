using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using erp.Services.Inventory;
using erp.DTOs.Inventory;

namespace erp.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IInventoryService inventoryService,
        ILogger<ProductsController> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    /// <summary>
    /// Busca produtos com filtros, ordenação e paginação
    /// </summary>
    [HttpPost("search")]
    public async Task<ActionResult> SearchProducts([FromBody] ProductSearchDto searchDto)
    {
        try
        {
            var (products, totalCount) = await _inventoryService.SearchProductsAsync(searchDto);
            
            var totalPages = (int)Math.Ceiling(totalCount / (double)searchDto.PageSize);
            
            return Ok(new
            {
                items = products,
                totalItems = totalCount,
                page = searchDto.Page,
                pageSize = searchDto.PageSize,
                totalPages,
                hasNextPage = searchDto.Page < totalPages,
                hasPreviousPage = searchDto.Page > 1
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar produtos");
            return StatusCode(500, new { message = "Erro ao buscar produtos", error = ex.Message });
        }
    }

    /// <summary>
    /// Lista produtos (simplificado - usa search internamente)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetProducts(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var searchDto = new ProductSearchDto
            {
                SearchTerm = search,
                Page = page,
                PageSize = pageSize,
                SortBy = "Name",
                SortDescending = false
            };

            return await SearchProducts(searchDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar produtos");
            return StatusCode(500, new { message = "Erro ao listar produtos", error = ex.Message });
        }
    }

    /// <summary>
    /// Busca produto por ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetProductById(int id)
    {
        try
        {
            var product = await _inventoryService.GetProductByIdAsync(id);
            
            if (product == null)
            {
                return NotFound(new { message = $"Produto com ID {id} não encontrado" });
            }

            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar produto {ProductId}", id);
            return StatusCode(500, new { message = "Erro ao buscar produto", error = ex.Message });
        }
    }

    /// <summary>
    /// Busca produto por SKU
    /// </summary>
    [HttpGet("sku/{sku}")]
    public async Task<ActionResult<ProductDto>> GetProductBySku(string sku)
    {
        try
        {
            var product = await _inventoryService.GetProductBySkuAsync(sku);
            
            if (product == null)
            {
                return NotFound(new { message = $"Produto com SKU '{sku}' não encontrado" });
            }

            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar produto por SKU {Sku}", sku);
            return StatusCode(500, new { message = "Erro ao buscar produto", error = ex.Message });
        }
    }

    /// <summary>
    /// Cria um novo produto
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductDto createDto)
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

            var product = await _inventoryService.CreateProductAsync(createDto, userId);
            
            return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Erro de validação ao criar produto");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar produto");
            return StatusCode(500, new { message = "Erro ao criar produto", error = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza um produto existente
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(int id, [FromBody] UpdateProductDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != updateDto.Id)
            {
                return BadRequest(new { message = "ID da URL não corresponde ao ID do produto" });
            }

            var product = await _inventoryService.UpdateProductAsync(updateDto);
            return Ok(product);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Erro de validação ao atualizar produto {ProductId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar produto {ProductId}", id);
            return StatusCode(500, new { message = "Erro ao atualizar produto", error = ex.Message });
        }
    }

    /// <summary>
    /// Deleta um produto
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteProduct(int id)
    {
        try
        {
            var success = await _inventoryService.DeleteProductAsync(id);
            
            if (!success)
            {
                return NotFound(new { message = $"Produto com ID {id} não encontrado" });
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Erro ao deletar produto {ProductId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar produto {ProductId}", id);
            return StatusCode(500, new { message = "Erro ao deletar produto", error = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza preços em massa de produtos
    /// </summary>
    [HttpPost("bulk-update-prices")]
    public async Task<ActionResult> BulkUpdatePrices([FromBody] BulkUpdatePriceDto bulkUpdateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!bulkUpdateDto.ProductIds.Any())
            {
                return BadRequest(new { message = "Lista de produtos vazia" });
            }

            var success = await _inventoryService.BulkUpdatePricesAsync(bulkUpdateDto);
            
            if (success)
            {
                return Ok(new 
                { 
                    message = "Preços atualizados com sucesso", 
                    productsUpdated = bulkUpdateDto.ProductIds.Count 
                });
            }
            
            return BadRequest(new { message = "Erro ao atualizar preços" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar preços em massa");
            return StatusCode(500, new { message = "Erro ao atualizar preços", error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém todos os alertas de estoque
    /// </summary>
    [HttpGet("alerts")]
    public async Task<ActionResult> GetStockAlerts()
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
}
