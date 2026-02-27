using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using erp.Services.Inventory;
using erp.DTOs.Inventory;
using erp.Models.Audit;
using erp.Security;

namespace erp.Controllers;

/// <summary>
/// Controller para gerenciamento de produtos e inventário
/// </summary>
[Authorize]
[ApiController]
[Route("api/produtos")]
public class ProductsController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly IFileValidationService _fileValidationService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IInventoryService inventoryService,
        ILogger<ProductsController> logger,
        IFileValidationService fileValidationService)
    {
        _inventoryService = inventoryService;
        _logger = logger;
        _fileValidationService = fileValidationService;
    }

    /// <summary>
    /// Busca produtos com filtros avançados, ordenação e paginação
    /// </summary>
    [HttpPost("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// Lista produtos de forma simplificada (wrapper para busca)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetProducts(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] bool? lowStock = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            // Parse status string to int if provided
            int? statusInt = status?.ToLower() switch
            {
                "ativo" => 0,
                "inativo" => 1,
                "descontinuado" => 2,
                _ => null
            };

            var searchDto = new ProductSearchDto
            {
                SearchTerm = search,
                Status = statusInt,
                CategoryId = categoryId,
                LowStock = lowStock,
                SortBy = sortBy ?? "Name",
                SortDescending = sortDescending,
                Page = page,
                PageSize = pageSize
            };

            var (products, totalCount) = await _inventoryService.SearchProductsAsync(searchDto);
            
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            
            return Ok(new
            {
                items = products,
                total = totalCount,
                page,
                pageSize,
                totalPages,
                hasNextPage = page < totalPages,
                hasPreviousPage = page > 1
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar produtos");
            return StatusCode(500, new { message = "Erro ao listar produtos", error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém estatísticas gerais de produtos
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProductStatisticsDto>> GetStatistics()
    {
        try
        {
            var statistics = await _inventoryService.GetProductStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter estatísticas de produtos");
            return StatusCode(500, new { message = "Erro ao obter estatísticas", error = ex.Message });
        }
    }

    /// <summary>
    /// Exporta produtos para CSV
    /// </summary>
    [HttpGet("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportProducts(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] bool? lowStock = null,
        [FromQuery] string format = "csv")
    {
        try
        {
            int? statusInt = status?.ToLower() switch
            {
                "ativo" => 0,
                "inativo" => 1,
                "descontinuado" => 2,
                _ => null
            };

            var searchDto = new ProductSearchDto
            {
                SearchTerm = search,
                Status = statusInt,
                CategoryId = categoryId,
                LowStock = lowStock
            };

            var bytes = await _inventoryService.ExportProductsAsync(searchDto, format);
            
            var fileName = $"produtos_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(bytes, "text/csv; charset=utf-8", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao exportar produtos");
            return StatusCode(500, new { message = "Erro ao exportar produtos", error = ex.Message });
        }
    }

    /// <summary>
    /// Busca produto por ID
    /// </summary>
    [HttpGet("{id:int}")]
    [AuditRead("Product", DataSensitivity.Low, Description = "Visualização de detalhes do produto (preços, estoque)")]
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
    /// Baixa um arquivo modelo para importacao de produtos
    /// </summary>
    [HttpGet("import/template")]
    public async Task<IActionResult> DownloadImportTemplate(CancellationToken cancellationToken)
    {
        try
        {
            var content = await _inventoryService.GenerateImportTemplateAsync(cancellationToken);
            var fileName = $"template_importacao_produtos_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar template de importacao de produtos");
            return StatusCode(500, new { message = "Erro ao gerar template de importacao", error = ex.Message });
        }
    }

    /// <summary>
    /// Importa produtos a partir de uma planilha Excel (.xlsx)
    /// </summary>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ProductImportResultDto>> ImportProducts(
        [FromForm] ImportProductsFormDto formData,
        CancellationToken cancellationToken)
    {
        try
        {
            if (formData.File == null || formData.File.Length == 0)
            {
                return BadRequest(new { message = "Nenhum arquivo foi enviado." });
            }

            var extension = Path.GetExtension(formData.File.FileName);
            if (!extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Formato invalido. Envie um arquivo .xlsx." });
            }

            var validationResult = await _fileValidationService.ValidateFileAsync(formData.File, cancellationToken);
            if (!validationResult.IsValid)
            {
                return BadRequest(new { message = validationResult.ErrorMessage ?? "Arquivo invalido." });
            }

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Usuario nao autenticado" });
            }

            await using var stream = formData.File.OpenReadStream();
            var result = await _inventoryService.ImportProductsFromExcelAsync(stream, userId, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Erro de validacao ao importar produtos");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao importar produtos");
            return StatusCode(500, new { message = "Erro ao importar produtos", error = ex.Message });
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

    #region Categories

    /// <summary>
    /// Lista categorias com paginação e filtros
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult> GetCategories(
        [FromQuery] string? search = null,
        [FromQuery] int? parentCategoryId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var (categories, totalCount) = await _inventoryService.GetCategoriesAsync(
                search, parentCategoryId, isActive, page, pageSize);
            
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            
            return Ok(new
            {
                items = categories,
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
            _logger.LogError(ex, "Erro ao listar categorias");
            return StatusCode(500, new { message = "Erro ao listar categorias", error = ex.Message });
        }
    }

    /// <summary>
    /// Busca categoria por ID
    /// </summary>
    [HttpGet("categories/{id:int}")]
    public async Task<ActionResult<ProductCategoryDto>> GetCategoryById(int id)
    {
        try
        {
            var category = await _inventoryService.GetCategoryByIdAsync(id);
            
            if (category == null)
            {
                return NotFound(new { message = $"Categoria com ID {id} não encontrada" });
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
    /// Cria uma nova categoria
    /// </summary>
    [HttpPost("categories")]
    public async Task<ActionResult<ProductCategoryDto>> CreateCategory([FromBody] CreateProductCategoryDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var category = await _inventoryService.CreateCategoryAsync(createDto);
            
            return CreatedAtAction(nameof(GetCategoryById), new { id = category.Id }, category);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Erro de validação ao criar categoria");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar categoria");
            return StatusCode(500, new { message = "Erro ao criar categoria", error = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza uma categoria existente
    /// </summary>
    [HttpPut("categories/{id:int}")]
    public async Task<ActionResult<ProductCategoryDto>> UpdateCategory(int id, [FromBody] UpdateProductCategoryDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != updateDto.Id)
            {
                return BadRequest(new { message = "ID da URL não corresponde ao ID da categoria" });
            }

            var category = await _inventoryService.UpdateCategoryAsync(updateDto);
            return Ok(category);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Erro de validação ao atualizar categoria {CategoryId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar categoria {CategoryId}", id);
            return StatusCode(500, new { message = "Erro ao atualizar categoria", error = ex.Message });
        }
    }

    /// <summary>
    /// Deleta uma categoria
    /// </summary>
    [HttpDelete("categories/{id:int}")]
    public async Task<ActionResult> DeleteCategory(int id)
    {
        try
        {
            var success = await _inventoryService.DeleteCategoryAsync(id);
            
            if (!success)
            {
                return NotFound(new { message = $"Categoria com ID {id} não encontrada" });
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Erro ao deletar categoria {CategoryId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar categoria {CategoryId}", id);
            return StatusCode(500, new { message = "Erro ao deletar categoria", error = ex.Message });
        }
    }

    #endregion

    #region Variants

    /// <summary>
    /// Lista variações de um produto
    /// </summary>
    [HttpGet("{id:int}/variantes")]
    public async Task<ActionResult<List<ProductVariantDto>>> GetProductVariants(int id)
    {
        try
        {
            var variants = await _inventoryService.GetProductVariantsAsync(id);
            return Ok(variants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar variações do produto {ProductId}", id);
            return StatusCode(500, new { message = "Erro ao listar variações", error = ex.Message });
        }
    }

    /// <summary>
    /// Cria uma opção de variação para o produto (ex: Cor, Tamanho)
    /// </summary>
    [HttpPost("{id:int}/variantes/opcoes")]
    public async Task<ActionResult<ProductVariantOptionDto>> CreateVariantOption(int id, [FromBody] CreateProductVariantOptionDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var option = await _inventoryService.CreateVariantOptionAsync(id, dto);
            return Ok(option);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Erro de validação ao criar opção de variação para produto {ProductId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar opção de variação para produto {ProductId}", id);
            return StatusCode(500, new { message = "Erro ao criar opção", error = ex.Message });
        }
    }

    /// <summary>
    /// Remove uma opção de variação
    /// </summary>
    [HttpDelete("{id:int}/variantes/opcoes/{optionId:int}")]
    public async Task<ActionResult> DeleteVariantOption(int id, int optionId)
    {
        try
        {
            var success = await _inventoryService.DeleteVariantOptionAsync(optionId);
            if (!success)
                return NotFound(new { message = $"Opção de variação com ID {optionId} não encontrada" });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Erro ao excluir opção de variação {OptionId}", optionId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao excluir opção de variação {OptionId}", optionId);
            return StatusCode(500, new { message = "Erro ao excluir opção", error = ex.Message });
        }
    }

    /// <summary>
    /// Cria uma variação concreta do produto
    /// </summary>
    [HttpPost("{id:int}/variantes")]
    public async Task<ActionResult<ProductVariantDto>> CreateVariant(int id, [FromBody] CreateProductVariantDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var variant = await _inventoryService.CreateVariantAsync(id, dto);
            return Ok(variant);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Erro de validação ao criar variação para produto {ProductId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar variação para produto {ProductId}", id);
            return StatusCode(500, new { message = "Erro ao criar variação", error = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza uma variação existente
    /// </summary>
    [HttpPut("{id:int}/variantes/{variantId:int}")]
    public async Task<ActionResult<ProductVariantDto>> UpdateVariant(int id, int variantId, [FromBody] UpdateProductVariantDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (variantId != dto.Id)
                return BadRequest(new { message = "ID da URL não corresponde ao ID da variação" });

            var variant = await _inventoryService.UpdateVariantAsync(variantId, dto);
            return Ok(variant);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Erro de validação ao atualizar variação {VariantId}", variantId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar variação {VariantId}", variantId);
            return StatusCode(500, new { message = "Erro ao atualizar variação", error = ex.Message });
        }
    }

    /// <summary>
    /// Remove uma variação
    /// </summary>
    [HttpDelete("{id:int}/variantes/{variantId:int}")]
    public async Task<ActionResult> DeleteVariant(int id, int variantId)
    {
        try
        {
            var success = await _inventoryService.DeleteVariantAsync(variantId);
            if (!success)
                return NotFound(new { message = $"Variação com ID {variantId} não encontrada" });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Erro ao excluir variação {VariantId}", variantId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao excluir variação {VariantId}", variantId);
            return StatusCode(500, new { message = "Erro ao excluir variação", error = ex.Message });
        }
    }

    #endregion
}
