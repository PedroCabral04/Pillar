using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using erp.DTOs.Financial;
using erp.Services.Financial;
using erp.Models.Financial;
using System.Security.Claims;

namespace erp.Controllers;

[ApiController]
[Route("api/accounts-payable")]
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
    /// Get paginated accounts payable with filters
    /// </summary>
    [HttpGet]
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
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false)
    {
        try
        {
            var result = await _accountPayableService.GetPagedAsync(
                page, pageSize, supplierId, status, dueDateFrom, dueDateTo,
                categoryId, costCenterId, pendingApproval, sortBy, sortDescending);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting accounts payable");
            return StatusCode(500, "Erro ao buscar contas a pagar");
        }
    }

    /// <summary>
    /// Get account payable by ID
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
    /// Get overdue accounts payable
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
    /// Get accounts payable due soon (within days)
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
    /// Get accounts pending approval
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
    /// Get total amounts by status
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
    /// Get total amounts by supplier
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
    /// Get installments of a parent account
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
    /// Create new account payable
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
    /// Update existing account payable
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
    /// Delete account payable
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
    /// Approve an account payable
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Administrador,Gerente")]
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
    /// Pay an account payable
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
                dto.ProofOfPaymentUrl, existing.BankSlipNumber, existing.PixKey);
            
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
    /// Create installments from a base account
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
    /// Update overdue status for all accounts (batch operation)
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
