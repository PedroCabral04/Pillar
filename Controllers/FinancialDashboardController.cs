using erp.DTOs.Financial;
using erp.Services.Financial;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace erp.Controllers;

[ApiController]
[Route("api/dashboard-financeiro")]
[Authorize]
public class FinancialDashboardController : ControllerBase
{
    private readonly IFinancialDashboardService _dashboardService;
    private readonly ILogger<FinancialDashboardController> _logger;

    public FinancialDashboardController(
        IFinancialDashboardService dashboardService,
        ILogger<FinancialDashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    [HttpGet]
    [HttpGet("/api/financial-dashboard")]
    public async Task<ActionResult<FinancialDashboardDto>> GetDashboardData([FromQuery] decimal initialBalance = 0)
    {
        try
        {
            var data = await _dashboardService.GetDashboardDataAsync(initialBalance: initialBalance);
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting financial dashboard data");
            return StatusCode(500, "Erro ao carregar dados do dashboard financeiro");
        }
    }
}
