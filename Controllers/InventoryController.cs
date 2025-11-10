using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using erp.Services.Inventory;
using erp.DTOs.Inventory;
using erp.Models.Audit;

namespace erp.Controllers;

/// <summary>
/// Controller para gerenciamento de inventário, movimentações de estoque e contagens físicas
/// </summary>
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
    /// <param name="status">Filtro por status (Pending, InProgress, Completed, Cancelled)</param>
    /// <param name="warehouseId">Filtro por armazém</param>
    /// <param name="page">Número da página (padrão: 1)</param>
    /// <param name="pageSize">Itens por página (padrão: 20)</param>
    /// <returns>Lista paginada de contagens de estoque</returns>
    /// <response code="200">Contagens listadas com sucesso</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    [HttpGet("counts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// Cria uma nova contagem de estoque (inventário físico)
    /// </summary>
    /// <param name="createDto">Dados da contagem: armazém, descrição e data de início</param>
    /// <returns>Contagem criada com status Pending</returns>
    /// <response code="201">Contagem criada com sucesso</response>
    /// <response code="400">Dados inválidos ou armazém não encontrado</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// A contagem é criada com status "Pending" e pode receber itens.
    /// Use POST /stock-counts/{id}/items para adicionar produtos à contagem.
    /// </remarks>
    [HttpPost("counts")]
    [ProducesResponseType(typeof(StockCountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// Busca contagem de estoque por ID com itens detalhados
    /// </summary>
    /// <param name="id">ID da contagem</param>
    /// <returns>Dados completos da contagem incluindo itens, divergências e status</returns>
    /// <response code="200">Contagem encontrada</response>
    /// <response code="404">Contagem não encontrada</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// Retorna informações detalhadas da contagem física de estoque:
    /// - Status da contagem (Pending, InProgress, Completed, Cancelled)
    /// - Lista de itens contados com quantidade sistema vs. física
    /// - Divergências e ajustes aplicados
    /// - Informações de aprovação (quem e quando)
    /// </remarks>
    [HttpGet("stock-counts/{id:int}")]
    [AuditRead("StockCount", DataSensitivity.Medium, Description = "Visualização de contagem de estoque física")]
    [ProducesResponseType(typeof(StockCountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// Lista contagens de estoque ativas (Pending ou InProgress)
    /// </summary>
    /// <returns>Lista de contagens que podem receber itens ou serem finalizadas</returns>
    /// <response code="200">Lista de contagens ativas retornada</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// Retorna apenas contagens com status "Pending" ou "InProgress".
    /// Útil para dashboards e telas de seleção rápida de contagens em andamento.
    /// </remarks>
    [HttpGet("stock-counts/active")]
    [ProducesResponseType(typeof(IEnumerable<StockCountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// Adiciona um produto à contagem de estoque física
    /// </summary>
    /// <param name="countId">ID da contagem</param>
    /// <param name="itemDto">Dados do item: productId, quantidadeContada, observações</param>
    /// <returns>Contagem atualizada com o novo item</returns>
    /// <response code="200">Item adicionado com sucesso</response>
    /// <response code="400">Contagem não está em status válido para adicionar itens ou produto não existe</response>
    /// <response code="404">Contagem ou produto não encontrado</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// Regras de negócio:
    /// - Contagem deve estar com status "Pending" ou "InProgress"
    /// - Produto deve existir e estar ativo
    /// - Quantidade contada será comparada com estoque do sistema
    /// - Sistema calcula automaticamente divergências (diferença entre contado e sistema)
    /// </remarks>
    [HttpPost("stock-counts/{countId:int}/items")]
    [ProducesResponseType(typeof(StockCountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// Aprova a contagem de estoque e aplica ajustes automáticos
    /// </summary>
    /// <param name="id">ID da contagem</param>
    /// <param name="approveDto">Dados de aprovação: observações e flag de aplicar ajustes</param>
    /// <returns>Contagem aprovada com status Completed</returns>
    /// <response code="200">Contagem aprovada e ajustes aplicados</response>
    /// <response code="400">Contagem não está em status válido para aprovação</response>
    /// <response code="404">Contagem não encontrada</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// **Ação crítica - Altera saldo de estoque!**
    /// 
    /// Processo de aprovação:
    /// 1. Valida que contagem está com status "InProgress" ou "Pending"
    /// 2. Compara quantidade contada vs. quantidade no sistema para cada item
    /// 3. Gera movimentações de estoque tipo "Adjustment" para corrigir divergências
    /// 4. Atualiza saldo de estoque de todos os produtos da contagem
    /// 5. Registra usuário aprovador e data/hora
    /// 6. Altera status para "Completed"
    /// 
    /// **Nota:** Ajustes são irreversíveis. Use com cautela.
    /// </remarks>
    [HttpPost("stock-counts/{id:int}/approve")]
    [ProducesResponseType(typeof(StockCountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// Cancela uma contagem de estoque em andamento
    /// </summary>
    /// <param name="id">ID da contagem</param>
    /// <returns>Confirmação de cancelamento</returns>
    /// <response code="200">Contagem cancelada com sucesso</response>
    /// <response code="400">Contagem já foi aprovada e não pode ser cancelada</response>
    /// <response code="404">Contagem não encontrada</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// Cancela uma contagem física de estoque sem aplicar ajustes.
    /// 
    /// Regras:
    /// - Contagem não pode estar com status "Completed" (já aprovada)
    /// - Itens da contagem são mantidos para auditoria
    /// - Status é alterado para "Cancelled"
    /// - Nenhum ajuste de estoque é aplicado
    /// </remarks>
    [HttpPost("stock-counts/{id:int}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// Lista movimentações de estoque com filtros avançados
    /// </summary>
    /// <param name="productId">Filtro por produto específico</param>
    /// <param name="movementType">Tipo de movimentação (In, Out, Adjustment, Transfer, Return)</param>
    /// <param name="warehouseId">Filtro por armazém de origem</param>
    /// <param name="startDate">Data inicial do período</param>
    /// <param name="endDate">Data final do período</param>
    /// <param name="page">Número da página (padrão: 1)</param>
    /// <param name="pageSize">Itens por página (padrão: 20)</param>
    /// <returns>Lista paginada de movimentações de estoque</returns>
    /// <response code="200">Movimentações listadas com sucesso</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// Tipos de movimentação:
    /// - **In**: Entrada de estoque (compras, devoluções de clientes)
    /// - **Out**: Saída de estoque (vendas, consumo)
    /// - **Adjustment**: Ajuste de inventário (correção de divergências)
    /// - **Transfer**: Transferência entre armazéns
    /// - **Return**: Devolução para fornecedor
    /// 
    /// Ordenação: Movimentações mais recentes primeiro.
    /// </remarks>
    [HttpGet("movements")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// Cria uma nova movimentação de estoque manual
    /// </summary>
    /// <param name="createDto">Dados da movimentação: produto, quantidade, tipo, armazém, motivo</param>
    /// <returns>Movimentação criada com saldo atualizado</returns>
    /// <response code="201">Movimentação criada e estoque atualizado</response>
    /// <response code="400">Dados inválidos, estoque insuficiente ou tipo inválido</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// **Ação crítica - Altera saldo de estoque em tempo real!**
    /// 
    /// Validações automáticas:
    /// - Produto deve existir e estar ativo
    /// - Armazém deve existir
    /// - Para saídas (Out), valida se há estoque suficiente
    /// - Quantidade deve ser maior que zero
    /// 
    /// Tipos de movimentação:
    /// - **In**: Adiciona ao estoque (quantidade positiva)
    /// - **Out**: Remove do estoque (valida disponibilidade)
    /// - **Adjustment**: Ajuste manual (pode ser positivo ou negativo)
    /// - **Transfer**: Transferência entre armazéns (requer armazém destino)
    /// - **Return**: Devolução (adiciona ao estoque)
    /// 
    /// Campo "Reason" é obrigatório para rastreabilidade.
    /// </remarks>
    [HttpPost("movements")]
    [ProducesResponseType(typeof(StockMovementDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// Busca movimentação de estoque por ID
    /// </summary>
    /// <param name="id">ID da movimentação</param>
    /// <returns>Dados completos da movimentação incluindo produto, armazém e usuário responsável</returns>
    /// <response code="200">Movimentação encontrada</response>
    /// <response code="404">Movimentação não encontrada</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    [HttpGet("movements/{id:int}")]
    [ProducesResponseType(typeof(StockMovementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// Lista armazéns/depósitos cadastrados
    /// </summary>
    /// <param name="isActive">Filtro por status (true=ativos, false=inativos, null=todos)</param>
    /// <param name="page">Número da página (padrão: 1)</param>
    /// <param name="pageSize">Itens por página (padrão: 100)</param>
    /// <returns>Lista de armazéns com informações de localização e capacidade</returns>
    /// <response code="200">Armazéns listados com sucesso</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// Retorna todos os armazéns cadastrados no sistema.
    /// Cada armazém pode ter múltiplos produtos com estoques independentes.
    /// </remarks>
    [HttpGet("warehouses")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// Obtém dashboard consolidado de alertas de estoque
    /// </summary>
    /// <returns>Resumo com contadores de produtos em situação crítica: estoque baixo, excesso e inativos</returns>
    /// <response code="200">Alertas retornados com sucesso</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// Retorna um resumo executivo de situações críticas de estoque:
    /// - **LowStock**: Produtos abaixo do estoque mínimo configurado
    /// - **Overstock**: Produtos acima do estoque máximo (capital parado)
    /// - **Inactive**: Produtos sem movimentação nos últimos 90 dias
    /// 
    /// Útil para dashboards gerenciais e tomada de decisão.
    /// </remarks>
    [HttpGet("alerts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// Lista produtos com estoque abaixo do mínimo configurado
    /// </summary>
    /// <returns>Lista de produtos que precisam de reposição urgente</returns>
    /// <response code="200">Produtos com estoque baixo retornados</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// Retorna produtos onde: **Estoque Atual &lt; Estoque Mínimo**
    /// 
    /// Cada produto inclui:
    /// - Quantidade atual em estoque
    /// - Estoque mínimo configurado
    /// - Diferença a ser reposta
    /// - Última movimentação
    /// 
    /// **Ação recomendada:** Gerar ordem de compra para reposição.
    /// </remarks>
    [HttpGet("alerts/low-stock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// Lista produtos com excesso de estoque (acima do máximo)
    /// </summary>
    /// <returns>Lista de produtos com capital parado em estoque</returns>
    /// <response code="200">Produtos com excesso de estoque retornados</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// Retorna produtos onde: **Estoque Atual &gt; Estoque Máximo**
    /// 
    /// Indica possível:
    /// - Compra excessiva
    /// - Capital parado
    /// - Risco de obsolescência
    /// - Custo de armazenagem elevado
    /// 
    /// **Ação recomendada:** Avaliar promoções ou redução de pedidos futuros.
    /// </remarks>
    [HttpGet("alerts/overstock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// Lista produtos sem movimentação (parados no estoque)
    /// </summary>
    /// <param name="days">Número de dias sem movimentação (padrão: 90 dias)</param>
    /// <returns>Lista de produtos inativos que podem estar obsoletos</returns>
    /// <response code="200">Produtos inativos retornados</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// Identifica produtos sem nenhuma movimentação (entrada ou saída) no período especificado.
    /// 
    /// Riscos de produtos inativos:
    /// - Obsolescência
    /// - Perda de validade
    /// - Capital imobilizado
    /// - Custo de armazenagem sem retorno
    /// 
    /// **Ação recomendada:** Avaliar desconto, doação, baixa contábil ou descarte.
    /// 
    /// **Exemplo:** GET /alerts/inactive?days=180 - produtos sem movimentação há 6 meses.
    /// </remarks>
    [HttpGet("alerts/inactive")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
