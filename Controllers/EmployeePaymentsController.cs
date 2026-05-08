using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using erp.DTOs.Financial;
using erp.Models.Financial;
using erp.Services.Financial;
using System.Security.Claims;

namespace erp.Controllers;

[ApiController]
[Route("api/employee-payments")]
[Authorize]
public class EmployeePaymentsController : ControllerBase
{
    private readonly IEmployeePaymentService _service;
    private readonly ILogger<EmployeePaymentsController> _logger;

    public EmployeePaymentsController(
        IEmployeePaymentService service,
        ILogger<EmployeePaymentsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? employeeId = null,
        [FromQuery] EmployeePaymentType? type = null,
        [FromQuery] AccountStatus? status = null,
        [FromQuery] int? referenceMonth = null,
        [FromQuery] int? referenceYear = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false)
    {
        try
        {
            var (items, totalCount) = await _service.GetPagedAsync(
                page, pageSize, employeeId, type, status, referenceMonth, referenceYear,
                sortBy, sortDescending);
            return Ok(new { Items = items, TotalCount = totalCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employee payments");
            return StatusCode(500, "Erro ao buscar pagamentos de funcionários");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EmployeePaymentDto>> GetById(int id)
    {
        try
        {
            var payment = await _service.GetByIdAsync(id);
            if (payment == null)
                return NotFound($"Pagamento com ID {id} não encontrado");
            return Ok(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employee payment {Id}", id);
            return StatusCode(500, "Erro ao buscar pagamento");
        }
    }

    [HttpGet("totals-by-status")]
    public async Task<ActionResult> GetTotalsByStatus()
    {
        try
        {
            var totals = await _service.GetTotalsByStatusAsync();
            return Ok(totals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting totals by status");
            return StatusCode(500, "Erro ao buscar totais por status");
        }
    }

    [HttpPost]
    public async Task<ActionResult<EmployeePaymentDto>> Create([FromBody] CreateEmployeePaymentDto dto)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var payment = await _service.CreateAsync(dto, userId.Value);
            return CreatedAtAction(nameof(GetById), new { id = payment.Id }, payment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating employee payment");
            return StatusCode(500, "Erro ao criar pagamento");
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<EmployeePaymentDto>> Update(int id, [FromBody] UpdateEmployeePaymentDto dto)
    {
        try
        {
            var payment = await _service.UpdateAsync(id, dto);
            return Ok(payment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating employee payment {Id}", id);
            return StatusCode(500, "Erro ao atualizar pagamento");
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting employee payment {Id}", id);
            return StatusCode(500, "Erro ao excluir pagamento");
        }
    }

    [HttpPost("{id}/pay")]
    public async Task<ActionResult<EmployeePaymentDto>> Pay(int id, [FromBody] PayEmployeePaymentDto dto)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var payment = await _service.PayAsync(id, dto, userId.Value);
            return Ok(payment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error paying employee payment {Id}", id);
            return StatusCode(500, "Erro ao registrar pagamento");
        }
    }

    [HttpPost("generate-service-payments")]
    public async Task<ActionResult<List<EmployeePaymentDto>>> GenerateServicePayments(
        [FromBody] GenerateServicePaymentsDto dto)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var payments = await _service.GenerateServicePaymentsAsync(dto, userId.Value);
            return Ok(payments);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating service payments");
            return StatusCode(500, "Erro ao gerar pagamentos por serviço");
        }
    }

    private int? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (claim != null && int.TryParse(claim, out var id))
            return id;
        return null;
    }
}
