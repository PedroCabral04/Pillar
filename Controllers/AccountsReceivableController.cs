using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using erp.DTOs.Financial;
using erp.Services.Financial;
using erp.Models.Financial;
using System.Security.Claims;

namespace erp.Controllers;

[ApiController]
[Route("api/contas-receber")]
[Authorize]
public class AccountsReceivableController : ControllerBase
{
    private readonly IAccountReceivableService _accountReceivableService;
    private readonly ILogger<AccountsReceivableController> _logger;

    public AccountsReceivableController(
        IAccountReceivableService accountReceivableService,
        ILogger<AccountsReceivableController> logger)
    {
        _accountReceivableService = accountReceivableService;
        _logger = logger;
    }

    /// <summary>
    /// Obtém contas a receber paginadas com filtros
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? customerId = null,
        [FromQuery] AccountStatus? status = null,
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
            var (items, totalCount) = await _accountReceivableService.GetPagedAsync(
                page, pageSize, customerId, status, dueDateFrom, dueDateTo,
                categoryId, costCenterId, sortBy, sortDescending, searchText);
            return Ok(new { Items = items, TotalCount = totalCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting accounts receivable");
            return StatusCode(500, "Erro ao buscar contas a receber");
        }
    }

    /// <summary>
    /// Obtém conta a receber por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AccountReceivableDto>> GetById(int id)
    {
        try
        {
            var account = await _accountReceivableService.GetByIdAsync(id);
            if (account == null)
            {
                return NotFound($"Conta a receber com ID {id} não encontrada");
            }
            return Ok(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account receivable {AccountId}", id);
            return StatusCode(500, "Erro ao buscar conta a receber");
        }
    }

    /// <summary>
    /// Obtém contas a receber vencidas
    /// </summary>
    [HttpGet("overdue")]
    public async Task<ActionResult<List<AccountReceivableDto>>> GetOverdue()
    {
        try
        {
            var accounts = await _accountReceivableService.GetOverdueAsync();
            return Ok(accounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overdue accounts");
            return StatusCode(500, "Erro ao buscar contas vencidas");
        }
    }

    /// <summary>
    /// Obtém contas a receber com vencimento em breve (dentro de X dias)
    /// </summary>
    [HttpGet("due-soon")]
    public async Task<ActionResult<List<AccountReceivableDto>>> GetDueSoon([FromQuery] int days = 7)
    {
        try
        {
            var accounts = await _accountReceivableService.GetDueSoonAsync(days);
            return Ok(accounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting accounts due soon");
            return StatusCode(500, "Erro ao buscar contas a vencer");
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
            var pending = await _accountReceivableService.GetTotalByStatusAsync(AccountStatus.Pending);
            var overdue = await _accountReceivableService.GetTotalByStatusAsync(AccountStatus.Overdue);
            var paid = await _accountReceivableService.GetTotalByStatusAsync(AccountStatus.Paid);
            var partiallyPaid = await _accountReceivableService.GetTotalByStatusAsync(AccountStatus.PartiallyPaid);
            var cancelled = await _accountReceivableService.GetTotalByStatusAsync(AccountStatus.Cancelled);
            
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
            var total = await _accountReceivableService.GetTotalByStatusAsync(status);
            return Ok(total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting totals by status");
            return StatusCode(500, "Erro ao calcular totais por status");
        }
    }

    /// <summary>
    /// Obtém o total de valores por cliente
    /// </summary>
    [HttpGet("totals/by-customer/{customerId}")]
    public async Task<ActionResult<decimal>> GetTotalByCustomer(int customerId)
    {
        try
        {
            var total = await _accountReceivableService.GetTotalByCustomerAsync(customerId);
            return Ok(total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting totals for customer {CustomerId}", customerId);
            return StatusCode(500, "Erro ao calcular totais do cliente");
        }
    }

    /// <summary>
    /// Obtém parcelas de uma conta pai
    /// </summary>
    [HttpGet("{parentId}/installments")]
    public async Task<ActionResult<List<AccountReceivableDto>>> GetInstallments(int parentId)
    {
        try
        {
            var installments = await _accountReceivableService.GetInstallmentsAsync(parentId);
            return Ok(installments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting installments for account {ParentId}", parentId);
            return StatusCode(500, "Erro ao buscar parcelas");
        }
    }

    /// <summary>
    /// Cria nova conta a receber
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AccountReceivableDto>> Create([FromBody] CreateAccountReceivableDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int currentUserId))
            {
                return Unauthorized();
            }

            var account = await _accountReceivableService.CreateAsync(dto, currentUserId);
            return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account receivable");
            return StatusCode(500, "Erro ao criar conta a receber");
        }
    }

    /// <summary>
    /// Atualiza conta a receber existente
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<AccountReceivableDto>> Update(int id, [FromBody] UpdateAccountReceivableDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int currentUserId))
            {
                return Unauthorized();
            }

            var account = await _accountReceivableService.UpdateAsync(id, dto, currentUserId);
            if (account == null)
            {
                return NotFound($"Conta a receber com ID {id} não encontrada");
            }
            return Ok(account);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating account receivable {AccountId}", id);
            return StatusCode(500, "Erro ao atualizar conta a receber");
        }
    }

    /// <summary>
    /// Exclui conta a receber
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            await _accountReceivableService.DeleteAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account receivable {AccountId}", id);
            return StatusCode(500, "Erro ao excluir conta a receber");
        }
    }

    /// <summary>
    /// Recebe pagamento de uma conta
    /// </summary>
    [HttpPost("{id}/receive")]
    public async Task<ActionResult<AccountReceivableDto>> ReceivePayment(
        int id,
        [FromBody] PayAccountReceivableDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int currentUserId))
            {
                return Unauthorized();
            }

            // Get the existing account to use its payment method
            var existing = await _accountReceivableService.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound($"Conta a receber com ID {id} não encontrada");
            }

            var account = await _accountReceivableService.ReceivePaymentAsync(
                id, dto.PaidAmount, existing.PaymentMethod, dto.PaymentDate, currentUserId,
                existing.BankSlipNumber, existing.PixKey,
                dto.AdditionalDiscount, dto.AdditionalInterest, dto.AdditionalFine);
            
            return Ok(account);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error receiving payment for account {AccountId}", id);
            return StatusCode(500, "Erro ao receber pagamento");
        }
    }

    /// <summary>
    /// Cria parcelas a partir de uma conta base
    /// </summary>
    [HttpPost("{id}/installments")]
    public async Task<ActionResult<List<AccountReceivableDto>>> CreateInstallments(
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
            var existing = await _accountReceivableService.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound($"Conta a receber com ID {id} não encontrada");
            }

            // Convert to CreateAccountReceivableDto
            var baseDto = new CreateAccountReceivableDto
            {
                CustomerId = existing.CustomerId,
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
                Notes = existing.Notes,
                InternalNotes = existing.InternalNotes
            };

            var installments = await _accountReceivableService.CreateInstallmentsAsync(
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
    /// Atualiza o status de vencimento para todas as contas (operação em lote)
    /// </summary>
    [HttpPost("update-overdue-status")]
    public async Task<ActionResult> UpdateOverdueStatus()
    {
        try
        {
            await _accountReceivableService.UpdateOverdueStatusAsync();
            return Ok("Status de contas vencidas atualizado com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating overdue status");
            return StatusCode(500, "Erro ao atualizar status de vencimento");
        }
    }
}
