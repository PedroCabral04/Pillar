using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using erp.DTOs.Sales;
using erp.Services.Sales;
using erp.Models.Audit;

namespace erp.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly ISalesService _salesService;
    private readonly ICustomerService _customerService;
    private readonly ILogger<SalesController> _logger;

    public SalesController(
        ISalesService salesService,
        ICustomerService customerService,
        ILogger<SalesController> logger)
    {
        _salesService = salesService;
        _customerService = customerService;
        _logger = logger;
    }

    #region Customers

    [HttpPost("customers")]
    public async Task<ActionResult<CustomerDto>> CreateCustomer([FromBody] CreateCustomerDto dto)
    {
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

    [HttpGet("customers/{id:int}")]
    [AuditRead("Customer", DataSensitivity.High, Description = "Visualização de dados do cliente (CPF/CNPJ, contatos)")]
    public async Task<ActionResult<CustomerDto>> GetCustomerById(int id)
    {
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

    [HttpGet("customers")]
    public async Task<ActionResult> SearchCustomers(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
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

    [HttpPut("customers/{id:int}")]
    public async Task<ActionResult<CustomerDto>> UpdateCustomer(int id, [FromBody] UpdateCustomerDto dto)
    {
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

    [HttpDelete("customers/{id:int}")]
    public async Task<ActionResult> DeleteCustomer(int id)
    {
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

    [HttpPost]
    public async Task<ActionResult<SaleDto>> CreateSale([FromBody] CreateSaleDto dto)
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

    [HttpGet("{id:int}")]
    [AuditRead("Sale", DataSensitivity.Medium, Description = "Visualização de dados da venda (valores, cliente)")]
    public async Task<ActionResult<SaleDto>> GetSaleById(int id)
    {
        try
        {
            var sale = await _salesService.GetByIdAsync(id);
            if (sale == null)
            {
                return NotFound(new { message = $"Venda com ID {id} não encontrada" });
            }
            return Ok(sale);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar venda {SaleId}", id);
            return StatusCode(500, new { message = "Erro ao buscar venda", error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult> SearchSales(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int? customerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var (items, total) = await _salesService.SearchAsync(
                search, status, startDate, endDate, customerId, page, pageSize);
            return Ok(new { items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar vendas");
            return StatusCode(500, new { message = "Erro ao buscar vendas", error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SaleDto>> UpdateSale(int id, [FromBody] UpdateSaleDto dto)
    {
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

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult> CancelSale(int id)
    {
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

    [HttpPost("{id:int}/finalize")]
    public async Task<ActionResult<SaleDto>> FinalizeSale(int id)
    {
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
}
