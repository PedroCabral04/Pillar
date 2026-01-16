using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using erp.Services.Inventory;
using erp.DTOs.Inventory;

namespace erp.Controllers;

/// <summary>
/// Controller para gerenciamento de movimentações de estoque (entradas, saídas e ajustes)
/// </summary>
[Authorize]
[ApiController]
[Route("api/movimentacoes-estoque")]
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
    /// <param name="createDto">Dados da movimentação: produto, quantidade, tipo, armazém, custo, documento</param>
    /// <returns>Movimentação criada com estoque atualizado</returns>
    /// <response code="201">Movimentação criada com sucesso</response>
    /// <response code="400">Dados inválidos ou estoque insuficiente para saída</response>
    /// <response code="404">Produto não encontrado</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// Endpoint genérico para criar qualquer tipo de movimentação.
    /// Para operações específicas, use os endpoints especializados:
    /// - POST /entry - Entrada de estoque
    /// - POST /exit - Saída de estoque
    /// - POST /adjustment - Ajuste de inventário
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(StockMovementDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// Cria uma movimentação de entrada de estoque (recebimento)
    /// </summary>
    /// <param name="createDto">Dados da entrada: produto, quantidade, custo unitário, nota fiscal, armazém</param>
    /// <returns>Entrada criada com estoque incrementado</returns>
    /// <response code="201">Entrada registrada e estoque atualizado</response>
    /// <response code="400">Dados inválidos (quantidade negativa ou custo inválido)</response>
    /// <response code="404">Produto não encontrado</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// Registra entrada de produtos no estoque (compras, devoluções de clientes, etc).
    /// 
    /// Operações automáticas:
    /// - Adiciona quantidade ao estoque atual do produto
    /// - Atualiza custo médio do produto (FIFO/LIFO conforme configuração)
    /// - Cria movimentação tipo "In" com timestamp
    /// - Associa número de documento (NF-e, CT-e, etc) para rastreabilidade
    /// 
    /// Campos obrigatórios:
    /// - ProductId: ID do produto
    /// - Quantity: Quantidade positiva
    /// - UnitCost: Custo unitário da entrada
    /// - DocumentNumber: Número da nota fiscal ou documento
    /// 
    /// **Exemplo de uso:** Recebimento de compra de fornecedor.
    /// </remarks>
    [HttpPost("entry")]
    [ProducesResponseType(typeof(StockMovementDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// Cria uma movimentação de saída de estoque (baixa)
    /// </summary>
    /// <param name="createDto">Dados da saída: produto, quantidade, documento, observações, armazém</param>
    /// <returns>Saída criada com estoque decrementado</returns>
    /// <response code="201">Saída registrada e estoque atualizado</response>
    /// <response code="400">Estoque insuficiente ou quantidade inválida</response>
    /// <response code="404">Produto não encontrado</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// Registra saída de produtos do estoque (vendas, consumo, transferências).
    /// 
    /// **Validação crítica:** Impede saída se quantidade solicitada for maior que estoque disponível.
    /// 
    /// Operações automáticas:
    /// - Valida disponibilidade de estoque antes de processar
    /// - Subtrai quantidade do estoque atual
    /// - Cria movimentação tipo "Out" com timestamp
    /// - Registra documento associado (NF-e de venda, requisição interna, etc)
    /// 
    /// Casos de uso:
    /// - **Venda**: Baixa automática ao confirmar venda
    /// - **Consumo**: Uso interno de materiais
    /// - **Devolução a fornecedor**: Retorno de mercadoria
    /// - **Transferência**: Saída de um armazém (requer entrada em outro)
    /// 
    /// **Erro comum 400:** "Estoque insuficiente" quando Quantity &gt; Stock Available.
    /// </remarks>
    [HttpPost("exit")]
    [ProducesResponseType(typeof(StockMovementDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// Cria uma movimentação de ajuste de estoque (correção manual)
    /// </summary>
    /// <param name="adjustmentDto">Dados do ajuste: produto, novo saldo, motivo, armazém</param>
    /// <returns>Ajuste criado com estoque corrigido</returns>
    /// <response code="201">Ajuste registrado e estoque corrigido</response>
    /// <response code="400">Dados inválidos ou motivo não informado</response>
    /// <response code="404">Produto não encontrado</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// **Ação crítica - Corrige estoque para um valor absoluto!**
    /// 
    /// Diferença entre ajuste e entrada/saída:
    /// - **Entrada/Saída**: Altera estoque relativamente (adiciona ou subtrai)
    /// - **Ajuste**: Define estoque absoluto (ignora valor anterior)
    /// 
    /// Processo:
    /// 1. Captura saldo atual do produto
    /// 2. Calcula diferença: NewStock - CurrentStock
    /// 3. Se diferença positiva: cria movimentação tipo "Adjustment In"
    /// 4. Se diferença negativa: cria movimentação tipo "Adjustment Out"
    /// 5. Atualiza estoque para o valor exato de NewStock
    /// 
    /// Casos de uso:
    /// - Correção após inventário físico (contagem)
    /// - Correção de erros de lançamento
    /// - Baixa de produtos danificados
    /// - Perdas por roubo ou extravio
    /// 
    /// **Campo "Reason" é obrigatório** para auditoria e rastreabilidade.
    /// 
    /// **Exemplo:** Se produto tem 100 unidades e NewStock=85, sistema cria ajuste de -15.
    /// </remarks>
    [HttpPost("adjustment")]
    [ProducesResponseType(typeof(StockMovementDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// Busca movimentação de estoque por ID
    /// </summary>
    /// <param name="id">ID da movimentação</param>
    /// <returns>Dados completos da movimentação incluindo produto, tipo, quantidade, documento e usuário</returns>
    /// <response code="200">Movimentação encontrada</response>
    /// <response code="404">Movimentação não encontrada</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(StockMovementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// Obtém histórico completo de movimentações de um produto
    /// </summary>
    /// <param name="productId">ID do produto</param>
    /// <param name="startDate">Data inicial do período (opcional)</param>
    /// <param name="endDate">Data final do período (opcional)</param>
    /// <returns>Lista cronológica de todas as movimentações do produto</returns>
    /// <response code="200">Histórico retornado com sucesso</response>
    /// <response code="404">Produto não encontrado</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// Retorna rastreabilidade completa de entradas, saídas e ajustes de um produto específico.
    /// 
    /// Informações incluídas em cada movimentação:
    /// - Tipo (In, Out, Adjustment, Transfer, Return)
    /// - Quantidade movimentada
    /// - Data e hora
    /// - Usuário responsável
    /// - Documento associado (NF-e, requisição, etc)
    /// - Saldo anterior e posterior
    /// - Custo unitário (quando aplicável)
    /// 
    /// Útil para:
    /// - Auditoria de estoque
    /// - Análise de giro de produto
    /// - Investigação de divergências
    /// - Relatórios gerenciais
    /// 
    /// Ordenação: Movimentações mais recentes primeiro.
    /// </remarks>
    [HttpGet("product/{productId:int}")]
    [ProducesResponseType(typeof(IEnumerable<StockMovementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
    /// Lista movimentações de estoque por período
    /// </summary>
    /// <param name="startDate">Data inicial (obrigatório)</param>
    /// <param name="endDate">Data final (obrigatório)</param>
    /// <param name="warehouseId">Filtro por armazém (opcional)</param>
    /// <returns>Lista de movimentações no período especificado</returns>
    /// <response code="200">Movimentações retornadas com sucesso</response>
    /// <response code="400">Data inicial posterior à data final</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// Relatório de movimentações para análise temporal de estoque.
    /// 
    /// Casos de uso:
    /// - Relatório mensal de movimentações
    /// - Análise de pico de demanda
    /// - Fechamento contábil de estoque
    /// - Auditoria por período
    /// 
    /// **Filtros disponíveis:**
    /// - Por data (obrigatório): startDate e endDate
    /// - Por armazém (opcional): warehouseId
    /// 
    /// **Exemplo:**
    /// 
    /// GET /date-range?startDate=2025-01-01&amp;endDate=2025-01-31&amp;warehouseId=1
    /// 
    /// Retorna todas as movimentações de janeiro/2025 do armazém 1.
    /// </remarks>
    [HttpGet("date-range")]
    [ProducesResponseType(typeof(IEnumerable<StockMovementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
