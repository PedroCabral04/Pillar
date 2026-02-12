using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using erp.DTOs.Financial;
using erp.Services.Financial;

namespace erp.Controllers;

/// <summary>
/// Controller para gerenciamento de performance de vendedores
/// </summary>
[ApiController]
[Route("api/vendor-performance")]
[Authorize]
public class VendorPerformanceController : ControllerBase
{
    private readonly IVendorPerformanceService _vendorPerformanceService;
    private readonly ILogger<VendorPerformanceController> _logger;

    public VendorPerformanceController(
        IVendorPerformanceService vendorPerformanceService,
        ILogger<VendorPerformanceController> logger)
    {
        _vendorPerformanceService = vendorPerformanceService;
        _logger = logger;
    }

    /// <summary>
    /// Obtém performance de um usuário em um período específico
    /// </summary>
    [HttpGet("user/{userId}/period")]
    [ProducesResponseType(typeof(VendorPerformanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VendorPerformanceDto>> GetUserPerformance(
        int userId,
        [FromQuery] int year,
        [FromQuery] int month)
    {
        try
        {
            var performance = await _vendorPerformanceService.GetByUserAndPeriodAsync(userId, year, month);
            return performance == null ? NotFound() : Ok(performance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance for user {UserId}", userId);
            return StatusCode(500, "Erro ao buscar performance");
        }
    }

    /// <summary>
    /// Obtém performance de todos os vendedores do tenant em um período
    /// </summary>
    [HttpGet("tenant/{tenantId}/period")]
    [ProducesResponseType(typeof(List<VendorPerformanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<VendorPerformanceDto>>> GetTenantPerformance(
        int tenantId,
        [FromQuery] int year,
        [FromQuery] int month)
    {
        try
        {
            var performances = await _vendorPerformanceService.GetByTenantAndPeriodAsync(tenantId, year, month);
            return Ok(performances);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performances for tenant {TenantId}", tenantId);
            return StatusCode(500, "Erro ao buscar performances");
        }
    }

    /// <summary>
    /// Obtém os top performers do tenant em um período
    /// </summary>
    [HttpGet("tenant/{tenantId}/top-performers")]
    [ProducesResponseType(typeof(List<VendorPerformanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<VendorPerformanceDto>>> GetTopPerformers(
        int tenantId,
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] int topN = 10)
    {
        try
        {
            var performers = await _vendorPerformanceService.GetTopPerformersAsync(tenantId, year, month, topN);
            return Ok(performers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top performers for tenant {TenantId}", tenantId);
            return StatusCode(500, "Erro ao buscar top performers");
        }
    }

    /// <summary>
    /// Recalcula a performance de um usuário para um período específico
    /// </summary>
    [HttpPost("user/{userId}/recalculate")]
    [ProducesResponseType(typeof(VendorPerformanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VendorPerformanceDto>> RecalculatePerformance(
        int userId,
        [FromQuery] int year,
        [FromQuery] int month)
    {
        try
        {
            var performance = await _vendorPerformanceService.CalculatePerformanceAsync(userId, year, month);
            return Ok(performance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating performance for user {UserId}", userId);
            return StatusCode(500, "Erro ao recalcular performance");
        }
    }

    /// <summary>
    /// Recalcula a performance de todos os vendedores do tenant para um período
    /// </summary>
    [HttpPost("tenant/{tenantId}/recalculate-period")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> RecalculatePeriod(
        int tenantId,
        [FromQuery] int year,
        [FromQuery] int month)
    {
        try
        {
            await _vendorPerformanceService.RecalculatePeriodAsync(tenantId, year, month);
            return Ok(new { message = $"Performance recalculada para o período {month}/{year}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating period for tenant {TenantId}", tenantId);
            return StatusCode(500, "Erro ao recalcular período");
        }
    }
}
