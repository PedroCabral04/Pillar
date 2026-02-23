using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using erp.DTOs.Financial;
using erp.Services.Financial;
using erp.Models.Financial;
using erp.Security;
using System.Security.Claims;

namespace erp.Controllers;

/// <summary>
/// Controller para gerenciamento de contas a pagar (fornecedores)
/// </summary>
[ApiController]
[Route("api/contas-pagar")]
[Authorize]
public class AccountsPayableController : ControllerBase
{
    private readonly IAccountPayableService _accountPayableService;
    private readonly ILogger<AccountsPayableController> _logger;

    public AccountsPayableController(
        IAccountPayableService accountPayableService,
        ILogger<AccountsPayableController> logger)
    {
        _accountPayableService = accountPayableService;
        _logger = logger;
    }

    /// <summary>
    /// Lista contas a pagar com paginação e filtros avançados
    /// </summary>
    /// <param name="page">Número da página (padrão: 1)</param>
    /// <param name="pageSize">Itens por página (padrão: 20)</param>
    /// <param name="supplierId">Filtro por fornecedor</param>
    /// <param name="status">Filtro por status (Pending, Paid, Overdue, Cancelled)</param>
    /// <param name="requiresApproval">Filtro por contas que requerem aprovação</param>
    /// <param name="pendingApproval">Filtro por contas pendentes de aprovação</param>
    /// <param name="dueDateFrom">Data de vencimento inicial</param>
    /// <param name="dueDateTo">Data de vencimento final</param>
    /// <param name="categoryId">Filtro por categoria financeira</param>
    /// <param name="costCenterId">Filtro por centro de custo</param>
    /// <param name="sortBy">Campo para ordenação</param>
    /// <param name="sortDescending">Ordenação descendente</param>
    /// <returns>Resultado paginado de contas a pagar</returns>
    /// <response code="200">Contas listadas com sucesso</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// Status disponíveis:
    /// - **Pending**: Aguardando pagamento (dentro do prazo)
    /// - **Paid**: Pago
    /// - **Overdue**: Vencido (não pago após data de vencimento)
    /// - **Cancelled**: Cancelado
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? supplierId = null,
        [FromQuery] AccountStatus? status = null,
        [FromQuery] bool? requiresApproval = null,
        [FromQuery] bool? pendingApproval = null,
        [FromQuery] DateTime? dueDateFrom = null,
        [FromQuery] DateTime? dueDateTo = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] int? costCenterId = null,
        [FromQuery] string? searchText = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false)
    {
        try
        {
            var (items, totalCount) = await _accountPayableService.GetPagedAsync(
                page, pageSize, supplierId, status, dueDateFrom, dueDateTo,
                categoryId, costCenterId, pendingApproval, sortBy, sortDescending, searchText);
            return Ok(new { Items = items, TotalCount = totalCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting accounts payable");
            return StatusCode(500, "Erro ao buscar contas a pagar");
        }
    }

    /// <summary>
    /// Obtém conta a pagar por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AccountPayableDto>> GetById(int id)
    {
        try
        {
            var account = await _accountPayableService.GetByIdAsync(id);
            if (account == null)
            {
                return NotFound($"Conta a pagar com ID {id} não encontrada");
            }
            return Ok(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account payable {AccountId}", id);
            return StatusCode(500, "Erro ao buscar conta a pagar");
        }
    }

    /// <summary>
    /// Obtém contas a pagar vencidas
    /// </summary>
    [HttpGet("overdue")]
    public async Task<ActionResult<List<AccountPayableDto>>> GetOverdue()
    {
        try
        {
            var accounts = await _accountPayableService.GetOverdueAsync();
            return Ok(accounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overdue accounts");
            return StatusCode(500, "Erro ao buscar contas vencidas");
        }
    }

    /// <summary>
    /// Obtém contas a pagar com vencimento em breve (dentro de X dias)
    /// </summary>
    [HttpGet("due-soon")]
    public async Task<ActionResult<List<AccountPayableDto>>> GetDueSoon([FromQuery] int days = 7)
    {
        try
        {
            var accounts = await _accountPayableService.GetDueSoonAsync(days);
            return Ok(accounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting accounts due soon");
            return StatusCode(500, "Erro ao buscar contas a vencer");
        }
    }

    /// <summary>
    /// Obtém contas pendentes de aprovação
    /// </summary>
    [HttpGet("pending-approval")]
    public async Task<ActionResult<List<AccountPayableDto>>> GetPendingApproval()
    {
        try
        {
            var accounts = await _accountPayableService.GetPendingApprovalAsync();
            return Ok(accounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting accounts pending approval");
            return StatusCode(500, "Erro ao buscar contas pendentes de aprovação");
        }
    }

    /// <summary>
    /// Obtém o resumo de totais por status
    /// </summary>
    [HttpGet("total-by-status")]
    public async Task<ActionResult> GetTotalsByStatus()
    {
        try
        {
            var pending = await _accountPayableService.GetTotalByStatusAsync(AccountStatus.Pending);
            var overdue = await _accountPayableService.GetTotalByStatusAsync(AccountStatus.Overdue);
            var paid = await _accountPayableService.GetTotalByStatusAsync(AccountStatus.Paid);
            var partiallyPaid = await _accountPayableService.GetTotalByStatusAsync(AccountStatus.PartiallyPaid);
            var cancelled = await _accountPayableService.GetTotalByStatusAsync(AccountStatus.Cancelled);
            
            return Ok(new { Pending = pending, Overdue = overdue, Paid = paid, PartiallyPaid = partiallyPaid, Cancelled = cancelled });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting totals by status");
            return StatusCode(500, "Erro ao calcular totais por status");
        }
    }

    /// <summary>
    /// Obtém o total de valores por status
    /// </summary>
    [HttpGet("totals/by-status/{status}")]
    public async Task<ActionResult<decimal>> GetTotalByStatus(AccountStatus status)
    {
        try
        {
            var total = await _accountPayableService.GetTotalByStatusAsync(status);
            return Ok(total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting totals by status");
            return StatusCode(500, "Erro ao calcular totais por status");
        }
    }

    /// <summary>
    /// Obtém o total de valores por fornecedor
    /// </summary>
    [HttpGet("totals/by-supplier/{supplierId}")]
    public async Task<ActionResult<decimal>> GetTotalBySupplier(int supplierId)
    {
        try
        {
            var total = await _accountPayableService.GetTotalBySupplierAsync(supplierId);
            return Ok(total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting totals for supplier {SupplierId}", supplierId);
            return StatusCode(500, "Erro ao calcular totais do fornecedor");
        }
    }

    /// <summary>
    /// Obtém parcelas de uma conta pai
    /// </summary>
    [HttpGet("{parentId}/installments")]
    public async Task<ActionResult<List<AccountPayableDto>>> GetInstallments(int parentId)
    {
        try
        {
            var installments = await _accountPayableService.GetInstallmentsAsync(parentId);
            return Ok(installments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting installments for account {ParentId}", parentId);
            return StatusCode(500, "Erro ao buscar parcelas");
        }
    }

    /// <summary>
    /// Cria nova conta a pagar
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AccountPayableDto>> Create([FromBody] CreateAccountPayableDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int currentUserId))
            {
                return Unauthorized();
            }

            var account = await _accountPayableService.CreateAsync(dto, currentUserId);
            return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account payable");
            return StatusCode(500, "Erro ao criar conta a pagar");
        }
    }

    /// <summary>
    /// Atualiza conta a pagar existente
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<AccountPayableDto>> Update(int id, [FromBody] UpdateAccountPayableDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int currentUserId))
            {
                return Unauthorized();
            }

            var account = await _accountPayableService.UpdateAsync(id, dto, currentUserId);
            if (account == null)
            {
                return NotFound($"Conta a pagar com ID {id} não encontrada");
            }
            return Ok(account);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating account payable {AccountId}", id);
            return StatusCode(500, "Erro ao atualizar conta a pagar");
        }
    }

    /// <summary>
    /// Exclui conta a pagar
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            await _accountPayableService.DeleteAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account payable {AccountId}", id);
            return StatusCode(500, "Erro ao excluir conta a pagar");
        }
    }

    /// <summary>
    /// Aprova uma conta a pagar
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize(Roles = RoleNames.AdminTenantSuperAdminOrManager)]
    public async Task<ActionResult<AccountPayableDto>> Approve(
        int id,
        [FromBody] ApproveAccountPayableDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int currentUserId))
            {
                return Unauthorized();
            }

            var account = await _accountPayableService.ApproveAsync(id, currentUserId, dto.ApprovalNotes);
            if (account == null)
            {
                return NotFound($"Conta a pagar com ID {id} não encontrada");
            }
            return Ok(account);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving account {AccountId}", id);
            return StatusCode(500, "Erro ao aprovar conta");
        }
    }

    /// <summary>
    /// Registra o pagamento de uma conta a pagar
    /// </summary>
    [HttpPost("{id}/pay")]
    public async Task<ActionResult<AccountPayableDto>> Pay(
        int id,
        [FromBody] PayAccountPayableDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int currentUserId))
            {
                return Unauthorized();
            }

            // Get the existing account to use its payment method
            var existing = await _accountPayableService.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound($"Conta a pagar com ID {id} não encontrada");
            }

            var account = await _accountPayableService.PayAsync(
                id, dto.PaidAmount, existing.PaymentMethod, dto.PaymentDate, currentUserId,
                dto.ProofOfPaymentUrl, existing.BankSlipNumber, existing.PixKey,
                dto.AdditionalDiscount, dto.AdditionalInterest, dto.AdditionalFine);
            
            return Ok(account);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error paying account {AccountId}", id);
            return StatusCode(500, "Erro ao pagar conta");
        }
    }

    /// <summary>
    /// Cria parcelas a partir de uma conta base
    /// </summary>
    [HttpPost("{id}/installments")]
    public async Task<ActionResult<List<AccountPayableDto>>> CreateInstallments(
        int id,
        [FromBody] CreateInstallmentsDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int currentUserId))
            {
                return Unauthorized();
            }

            // Get the existing account to use as base
            var existing = await _accountPayableService.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound($"Conta a pagar com ID {id} não encontrada");
            }

            // Convert to CreateAccountPayableDto
            var baseDto = new CreateAccountPayableDto
            {
                SupplierId = existing.SupplierId,
                InvoiceNumber = existing.InvoiceNumber,
                OriginalAmount = existing.OriginalAmount,
                DiscountAmount = existing.DiscountAmount,
                InterestAmount = existing.InterestAmount,
                FineAmount = existing.FineAmount,
                IssueDate = existing.IssueDate,
                DueDate = existing.DueDate,
                PaymentMethod = existing.PaymentMethod,
                BankSlipNumber = existing.BankSlipNumber,
                PixKey = existing.PixKey,
                CategoryId = existing.CategoryId,
                CostCenterId = existing.CostCenterId,
                InvoiceAttachmentUrl = existing.InvoiceAttachmentUrl,
                Notes = existing.Notes,
                InternalNotes = existing.InternalNotes
            };

            var installments = await _accountPayableService.CreateInstallmentsAsync(
                baseDto, dto.NumberOfInstallments, currentUserId, dto.InterestRate);
            
            return Ok(installments);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating installments for account {AccountId}", id);
            return StatusCode(500, "Erro ao criar parcelas");
        }
    }

    /// <summary>
    /// Atualiza o status de vencimento de todas as contas (operação em lote)
    /// </summary>
    [HttpPost("update-overdue-status")]
    public async Task<ActionResult> UpdateOverdueStatus()
    {
        try
        {
            await _accountPayableService.UpdateOverdueStatusAsync();
            return Ok("Status de contas vencidas atualizado com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating overdue status");
            return StatusCode(500, "Erro ao atualizar status de vencimento");
        }
    }
}
