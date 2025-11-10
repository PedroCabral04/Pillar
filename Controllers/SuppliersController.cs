using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using erp.DTOs.Financial;
using erp.Services.Financial;
using erp.Services.Financial.Validation;
using System.Security.Claims;

namespace erp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;
    private readonly ILogger<SuppliersController> _logger;

    public SuppliersController(ISupplierService supplierService, ILogger<SuppliersController> logger)
    {
        _supplierService = supplierService;
        _logger = logger;
    }

    /// <summary>
    /// Get all suppliers with optional pagination, search, and sorting
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetAll(
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null,
        [FromQuery] string? search = null,
        [FromQuery] bool? activeOnly = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false)
    {
        try
        {
            if (page.HasValue && pageSize.HasValue)
            {
                var result = await _supplierService.GetPagedAsync(page.Value, pageSize.Value, search, activeOnly, sortBy, sortDescending);
                return Ok(result);
            }
            else
            {
                var suppliers = await _supplierService.GetAllAsync(activeOnly ?? true);
                return Ok(suppliers);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suppliers");
            return StatusCode(500, "Erro ao buscar fornecedores");
        }
    }

    /// <summary>
    /// Get supplier by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<SupplierDto>> GetById(int id)
    {
        try
        {
            var supplier = await _supplierService.GetByIdAsync(id);
            if (supplier == null)
            {
                return NotFound($"Fornecedor com ID {id} não encontrado");
            }
            return Ok(supplier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supplier {SupplierId}", id);
            return StatusCode(500, "Erro ao buscar fornecedor");
        }
    }

    /// <summary>
    /// Create new supplier
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SupplierDto>> Create([FromBody] CreateSupplierDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int currentUserId))
            {
                return Unauthorized();
            }

            var supplier = await _supplierService.CreateAsync(dto, currentUserId);
            return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, supplier);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating supplier");
            return StatusCode(500, "Erro ao criar fornecedor");
        }
    }

    /// <summary>
    /// Update existing supplier
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<SupplierDto>> Update(int id, [FromBody] UpdateSupplierDto dto)
    {
        try
        {
            var supplier = await _supplierService.UpdateAsync(id, dto);
            if (supplier == null)
            {
                return NotFound($"Fornecedor com ID {id} não encontrado");
            }
            return Ok(supplier);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating supplier {SupplierId}", id);
            return StatusCode(500, "Erro ao atualizar fornecedor");
        }
    }

    /// <summary>
    /// Delete supplier
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            await _supplierService.DeleteAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting supplier {SupplierId}", id);
            return StatusCode(500, "Erro ao excluir fornecedor");
        }
    }

    /// <summary>
    /// Get company data from ReceitaWS by CNPJ
    /// </summary>
    [HttpGet("receita-ws/{cnpj}")]
    public async Task<ActionResult<ReceitaWsResponse>> GetCompanyData(string cnpj)
    {
        try
        {
            var data = await _supplierService.GetCompanyDataAsync(cnpj);
            if (data == null)
            {
                return NotFound($"Dados não encontrados para o CNPJ {cnpj}");
            }
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting company data for CNPJ {Cnpj}", cnpj);
            return StatusCode(500, "Erro ao consultar dados da empresa");
        }
    }

    /// <summary>
    /// Get address from ViaCEP by CEP
    /// </summary>
    [HttpGet("viacep/{cep}")]
    public async Task<ActionResult<ViaCepResponse>> GetAddress(string cep)
    {
        try
        {
            var data = await _supplierService.GetAddressAsync(cep);
            if (data == null)
            {
                return NotFound($"Endereço não encontrado para o CEP {cep}");
            }
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting address for CEP {Cep}", cep);
            return StatusCode(500, "Erro ao consultar endereço");
        }
    }
}
