using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using erp.DTOs.Sales;
using erp.Models.Identity;
using erp.Services.Sales;
using erp.Services.Authorization;
using erp.Services.Reports;
using erp.Models.Audit;

namespace erp.Controllers;

/// <summary>
/// Controller para gerenciamento de vendas e clientes
/// </summary>
[Authorize]
[ApiController]
[Route("api/vendas")]
public class SalesController : ControllerBase
{
    private readonly ISalesService _salesService;
    private readonly ICustomerService _customerService;
    private readonly IPdfExportService _pdfExportService;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<SalesController> _logger;

    public SalesController(
        ISalesService salesService,
        ICustomerService customerService,
        IPdfExportService pdfExportService,
        IWebHostEnvironment webHostEnvironment,
        IPermissionService permissionService,
        ILogger<SalesController> logger)
    {
        _salesService = salesService;
        _customerService = customerService;
        _pdfExportService = pdfExportService;
        _webHostEnvironment = webHostEnvironment;
        _permissionService = permissionService;
        _logger = logger;
    }

    #region Customers

    /// <summary>
    /// Cria um novo cliente no sistema
    /// </summary>
    /// <param name="dto">Dados do cliente incluindo informações fiscais (CPF/CNPJ) e de contato</param>
    /// <returns>Cliente criado com ID gerado</returns>
    /// <response code="201">Cliente criado com sucesso</response>
    /// <response code="400">Dados inválidos ou CPF/CNPJ já cadastrado</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno ao criar cliente</response>
    /// <remarks>
    /// Valida automaticamente CPF/CNPJ brasileiro.
    /// Nome e documento (CPF/CNPJ) são obrigatórios.
    /// 
    /// Exemplo de requisição:
    /// 
    ///     POST /api/sales/customers
    ///     {
    ///         "name": "João Silva",
    ///         "document": "123.456.789-00",
    ///         "email": "joao@exemplo.com",
    ///         "phone": "+55 11 98765-4321",
    ///         "address": "Rua Exemplo, 123",
    ///         "city": "São Paulo",
    ///         "state": "SP"
    ///     }
    /// </remarks>
    [HttpPost("customers")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CustomerDto>> CreateCustomer([FromBody] CreateCustomerDto dto)
    {
        var denied = await EnsureSalesActionAsync(ModuleActionKeys.Sales.ManageCustomers, "Você não tem permissão para gerenciar clientes.");
        if (denied != null)
            return denied;

        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var customer = await _customerService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetCustomerById), new { id = customer.Id }, customer);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar cliente");
            return StatusCode(500, new { message = "Erro ao criar cliente", error = ex.Message });
        }
    }

    /// <summary>
    /// Busca um cliente específico por ID
    /// </summary>
    /// <param name="id">ID do cliente</param>
    /// <returns>Dados completos do cliente</returns>
    /// <response code="200">Cliente encontrado</response>
    /// <response code="404">Cliente não encontrado</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// **ATENÇÃO:** Este endpoint retorna dados sensíveis (CPF/CNPJ) e é auditado.
    /// Toda consulta é registrada no log de auditoria com nível de sensibilidade ALTO.
    /// </remarks>
    [HttpGet("customers/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [AuditRead("Customer", DataSensitivity.High, Description = "Visualização de dados do cliente (CPF/CNPJ, contatos)")]
    public async Task<ActionResult<CustomerDto>> GetCustomerById(int id)
    {
        var denied = await EnsureSalesActionAsync(ModuleActionKeys.Sales.ManageCustomers, "Você não tem permissão para visualizar clientes.");
        if (denied != null)
            return denied;

        try
        {
            var customer = await _customerService.GetByIdAsync(id);
            if (customer == null)
            {
                return NotFound(new { message = $"Cliente com ID {id} não encontrado" });
            }
            return Ok(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar cliente {CustomerId}", id);
            return StatusCode(500, new { message = "Erro ao buscar cliente", error = ex.Message });
        }
    }

    /// <summary>
    /// Busca clientes com filtros e paginação
    /// </summary>
    /// <param name="search">Termo de busca (nome, documento, email)</param>
    /// <param name="isActive">Filtro por status ativo/inativo</param>
    /// <param name="page">Número da página (padrão: 1)</param>
    /// <param name="pageSize">Itens por página (padrão: 10)</param>
    /// <returns>Lista paginada de clientes</returns>
    /// <response code="200">Clientes encontrados</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// Busca por nome, CPF/CNPJ ou email do cliente.
    /// 
    /// Exemplo de uso:
    /// 
    ///     GET /api/sales/customers?search=João&amp;isActive=true&amp;page=1&amp;pageSize=10
    /// </remarks>
    [HttpGet("customers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> SearchCustomers(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var denied = await EnsureSalesActionAsync(ModuleActionKeys.Sales.ManageCustomers, "Você não tem permissão para visualizar clientes.");
        if (denied != null)
            return denied;

        try
        {
            var (items, total) = await _customerService.SearchAsync(search, isActive, page, pageSize);
            return Ok(new { items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar clientes");
            return StatusCode(500, new { message = "Erro ao buscar clientes", error = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza dados de um cliente existente
    /// </summary>
    /// <param name="id">ID do cliente</param>
    /// <param name="dto">Dados atualizados do cliente</param>
    /// <returns>Cliente atualizado</returns>
    /// <response code="200">Cliente atualizado com sucesso</response>
    /// <response code="400">Dados inválidos</response>
    /// <response code="404">Cliente não encontrado</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    [HttpPut("customers/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CustomerDto>> UpdateCustomer(int id, [FromBody] UpdateCustomerDto dto)
    {
        var denied = await EnsureSalesActionAsync(ModuleActionKeys.Sales.ManageCustomers, "Você não tem permissão para editar clientes.");
        if (denied != null)
            return denied;

        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var customer = await _customerService.UpdateAsync(id, dto);
            return Ok(customer);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar cliente {CustomerId}", id);
            return StatusCode(500, new { message = "Erro ao atualizar cliente", error = ex.Message });
        }
    }

    /// <summary>
    /// Inativa um cliente (soft delete)
    /// </summary>
    /// <param name="id">ID do cliente</param>
    /// <returns>Confirmação de inativação</returns>
    /// <response code="200">Cliente inativado com sucesso</response>
    /// <response code="404">Cliente não encontrado</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// **Nota:** Este endpoint realiza soft delete, mantendo o registro no banco mas marcando como inativo.
    /// O cliente não será excluído permanentemente e pode ser reativado posteriormente.
    /// </remarks>
    [HttpDelete("customers/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteCustomer(int id)
    {
        var denied = await EnsureSalesActionAsync(ModuleActionKeys.Sales.ManageCustomers, "Você não tem permissão para remover clientes.");
        if (denied != null)
            return denied;

        try
        {
            var result = await _customerService.DeleteAsync(id);
            if (!result)
            {
                return NotFound(new { message = $"Cliente com ID {id} não encontrado" });
            }
            return Ok(new { message = "Cliente inativado com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar cliente {CustomerId}", id);
            return StatusCode(500, new { message = "Erro ao deletar cliente", error = ex.Message });
        }
    }

    #endregion

    #region Sales

    /// <summary>
    /// Cria uma nova venda no sistema
    /// </summary>
    /// <param name="dto">Dados da venda incluindo cliente, itens, valores e forma de pagamento</param>
    /// <returns>Venda criada com ID gerado</returns>
    /// <response code="201">Venda criada com sucesso</response>
    /// <response code="400">Dados inválidos ou estoque insuficiente</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// Cria uma venda vinculada ao usuário autenticado como vendedor.
    /// Valida disponibilidade de estoque para todos os produtos.
    /// Calcula automaticamente subtotal, descontos e total.
    /// 
    /// Exemplo de requisição:
    /// 
    ///     POST /api/sales
    ///     {
    ///         "customerId": 5,
    ///         "saleDate": "2025-11-10T10:30:00Z",
    ///         "paymentMethod": "Cartão de Crédito",
    ///         "discount": 50.00,
    ///         "items": [
    ///             {
    ///                 "productId": 10,
    ///                 "quantity": 2,
    ///                 "unitPrice": 150.00
    ///             }
    ///         ]
    ///     }
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SaleDto>> CreateSale([FromBody] CreateSaleDto dto)
    {
        var denied = await EnsureSalesActionAsync(ModuleActionKeys.Common.Create, "Você não tem permissão para criar vendas.");
        if (denied != null)
            return denied;

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

            var sale = await _salesService.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetSaleById), new { id = sale.Id }, sale);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar venda");
            return StatusCode(500, new { message = "Erro ao criar venda", error = ex.Message });
        }
    }

    /// <summary>
    /// Busca uma venda específica por ID
    /// </summary>
    /// <param name="id">ID da venda</param>
    /// <returns>Dados completos da venda incluindo itens e cliente</returns>
    /// <response code="200">Venda encontrada</response>
    /// <response code="404">Venda não encontrada</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// **ATENÇÃO:** Este endpoint retorna dados sensíveis da venda e é auditado.
    /// Registra a visualização no log de auditoria com nível de sensibilidade MÉDIO.
    /// </remarks>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [AuditRead("Sale", DataSensitivity.Medium, Description = "Visualização de dados da venda (valores, cliente)")]
    public async Task<ActionResult<SaleDto>> GetSaleById(int id)
    {
        var denied = await EnsureSalesActionAsync(ModuleActionKeys.Sales.ViewHistory, "Você não tem permissão para visualizar histórico de vendas.");
        if (denied != null)
            return denied;

        try
        {
            var sale = await _salesService.GetByIdAsync(id);
            if (sale == null)
            {
                return NotFound(new { message = $"Venda com ID {id} não encontrada" });
            }

            var canViewValues = await HasSalesActionAsync(ModuleActionKeys.Sales.ViewValues);
            if (!canViewValues)
            {
                RedactSensitiveValues(sale);
            }

            return Ok(sale);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar venda {SaleId}", id);
            return StatusCode(500, new { message = "Erro ao buscar venda", error = ex.Message });
        }
    }

    /// <summary>
    /// Exporta uma venda para PDF
    /// </summary>
    /// <param name="id">ID da venda</param>
    /// <returns>Arquivo PDF da venda</returns>
    /// <response code="200">PDF gerado com sucesso</response>
    /// <response code="404">Venda não encontrada</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// Gera um PDF formatado da venda com:
    /// - Cabeçalho com logo da empresa
    /// - Informações da venda e cliente
    /// - Tabela de itens
    /// - Totais e observações
    /// </remarks>
    [HttpGet("{id:int}/export/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportSaleToPdf(int id)
    {
        var denied = await EnsureSalesActionAsync(ModuleActionKeys.Sales.ExportPdf, "Você não tem permissão para exportar vendas em PDF.");
        if (denied != null)
            return denied;

        try
        {
            var sale = await _salesService.GetByIdAsync(id);
            if (sale == null)
            {
                return NotFound(new { message = $"Venda com ID {id} não encontrada" });
            }

            // Try to find company logo
            var logoPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "logo.png");
            if (!System.IO.File.Exists(logoPath))
            {
                logoPath = null;
            }

            var pdfBytes = _pdfExportService.ExportSaleToPdf(sale, logoPath);
            return File(pdfBytes, "application/pdf", $"venda-{sale.SaleNumber}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao exportar venda {SaleId} para PDF", id);
            return StatusCode(500, new { message = "Erro ao exportar PDF", error = ex.Message });
        }
    }

    /// <summary>
    /// Busca vendas com filtros avançados e paginação
    /// </summary>
    /// <param name="search">Termo de busca (número da venda, nome do cliente)</param>
    /// <param name="status">Filtro por status (Draft, Confirmed, Shipped, Completed, Cancelled)</param>
    /// <param name="startDate">Data inicial do período</param>
    /// <param name="endDate">Data final do período</param>
    /// <param name="customerId">Filtro por cliente específico</param>
    /// <param name="page">Número da página (padrão: 1)</param>
    /// <param name="pageSize">Itens por página (padrão: 10)</param>
    /// <returns>Lista paginada de vendas</returns>
    /// <response code="200">Vendas encontradas</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// Permite filtrar vendas por múltiplos critérios simultaneamente.
    /// 
    /// Status disponíveis:
    /// - **Draft**: Rascunho (ainda não confirmada)
    /// - **Confirmed**: Confirmada (aguardando envio)
    /// - **Shipped**: Enviada (em transporte)
    /// - **Completed**: Concluída (entregue)
    /// - **Cancelled**: Cancelada
    /// 
    /// Exemplo de uso:
    /// 
    ///     GET /api/sales?status=Completed&amp;startDate=2025-01-01&amp;endDate=2025-12-31&amp;page=1
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> SearchSales(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int? customerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var denied = await EnsureSalesActionAsync(ModuleActionKeys.Sales.ViewHistory, "Você não tem permissão para visualizar histórico de vendas.");
        if (denied != null)
            return denied;

        try
        {
            var (items, total) = await _salesService.SearchAsync(
                search, status, startDate, endDate, customerId, page, pageSize);

            var canViewValues = await HasSalesActionAsync(ModuleActionKeys.Sales.ViewValues);
            if (!canViewValues)
            {
                foreach (var sale in items)
                {
                    RedactSensitiveValues(sale);
                }
            }

            return Ok(new { items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar vendas");
            return StatusCode(500, new { message = "Erro ao buscar vendas", error = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza dados de uma venda existente
    /// </summary>
    /// <param name="id">ID da venda</param>
    /// <param name="dto">Dados atualizados da venda</param>
    /// <returns>Venda atualizada</returns>
    /// <response code="200">Venda atualizada com sucesso</response>
    /// <response code="400">Dados inválidos ou operação não permitida (venda finalizada/cancelada)</response>
    /// <response code="404">Venda não encontrada</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// **Restrições:**
    /// - Não é possível atualizar vendas com status "Completed" ou "Cancelled"
    /// - Alterações em itens podem afetar o estoque
    /// </remarks>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SaleDto>> UpdateSale(int id, [FromBody] UpdateSaleDto dto)
    {
        var denied = await EnsureSalesActionAsync(ModuleActionKeys.Common.Update, "Você não tem permissão para editar vendas.");
        if (denied != null)
            return denied;

        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var sale = await _salesService.UpdateAsync(id, dto);
            return Ok(sale);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar venda {SaleId}", id);
            return StatusCode(500, new { message = "Erro ao atualizar venda", error = ex.Message });
        }
    }

    /// <summary>
    /// Cancela uma venda
    /// </summary>
    /// <param name="id">ID da venda</param>
    /// <returns>Confirmação de cancelamento</returns>
    /// <response code="200">Venda cancelada com sucesso</response>
    /// <response code="400">Operação não permitida (venda já finalizada)</response>
    /// <response code="404">Venda não encontrada</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// Altera o status da venda para "Cancelled".
    /// **Importante:** O estoque dos produtos é restaurado automaticamente.
    /// Não é possível cancelar vendas já completadas.
    /// </remarks>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> CancelSale(int id)
    {
        var denied = await EnsureSalesActionAsync(ModuleActionKeys.Sales.Cancel, "Você não tem permissão para cancelar vendas.");
        if (denied != null)
            return denied;

        try
        {
            var result = await _salesService.CancelAsync(id);
            if (!result)
            {
                return NotFound(new { message = $"Venda com ID {id} não encontrada" });
            }
            return Ok(new { message = "Venda cancelada com sucesso" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao cancelar venda {SaleId}", id);
            return StatusCode(500, new { message = "Erro ao cancelar venda", error = ex.Message });
        }
    }

    /// <summary>
    /// Finaliza uma venda marcando como concluída
    /// </summary>
    /// <param name="id">ID da venda</param>
    /// <returns>Venda finalizada</returns>
    /// <response code="200">Venda finalizada com sucesso</response>
    /// <response code="400">Operação não permitida (venda cancelada ou já finalizada)</response>
    /// <response code="404">Venda não encontrada</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// Altera o status da venda para "Completed".
    /// **Após finalizar, a venda não pode mais ser editada ou cancelada.**
    /// Use este endpoint apenas quando a entrega for confirmada.
    /// </remarks>
    [HttpPost("{id:int}/finalize")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SaleDto>> FinalizeSale(int id)
    {
        var denied = await EnsureSalesActionAsync(ModuleActionKeys.Sales.Finalize, "Você não tem permissão para finalizar vendas.");
        if (denied != null)
            return denied;

        try
        {
            var sale = await _salesService.FinalizeAsync(id);
            return Ok(sale);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao finalizar venda {SaleId}", id);
            return StatusCode(500, new { message = "Erro ao finalizar venda", error = ex.Message });
        }
    }

    #endregion

    private async Task<ActionResult?> EnsureSalesActionAsync(string actionKey, string message)
    {
        var hasAccess = await HasSalesActionAsync(actionKey);
        if (hasAccess)
            return null;

        return StatusCode(StatusCodes.Status403Forbidden, new { message });
    }

    private async Task<bool> HasSalesActionAsync(string actionKey)
    {
        return await _permissionService.HasModuleActionAccessAsync(User, ModuleKeys.Sales, actionKey);
    }

    private static void RedactSensitiveValues(SaleDto sale)
    {
        sale.TotalAmount = 0;
        sale.DiscountAmount = 0;
        sale.NetAmount = 0;

        if (sale.Items.Count == 0)
            return;

        foreach (var item in sale.Items)
        {
            item.UnitPrice = 0;
            item.Discount = 0;
            item.Total = 0;
        }
    }
}
