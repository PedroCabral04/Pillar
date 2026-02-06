using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using erp.DTOs.Reports;
using erp.Services.Reports;

namespace erp.Controllers;

[ApiController]
[Route("api/relatorios")]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly ISalesReportService _salesReportService;
    private readonly IFinancialReportService _financialReportService;
    private readonly IInventoryReportService _inventoryReportService;
    private readonly IHRReportService _hrReportService;
    private readonly IPdfExportService _pdfExportService;
    private readonly IExcelExportService _excelExportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        ISalesReportService salesReportService,
        IFinancialReportService financialReportService,
        IInventoryReportService inventoryReportService,
        IHRReportService hrReportService,
        IPdfExportService pdfExportService,
        IExcelExportService excelExportService,
        ILogger<ReportsController> logger)
    {
        _salesReportService = salesReportService;
        _financialReportService = financialReportService;
        _inventoryReportService = inventoryReportService;
        _hrReportService = hrReportService;
        _pdfExportService = pdfExportService;
        _excelExportService = excelExportService;
        _logger = logger;
    }

    #region Sales Reports

    /// <summary>
    /// Gera relatório de vendas
    /// </summary>
    [HttpPost("sales")]
    public async Task<ActionResult<SalesReportResultDto>> GenerateSalesReport([FromBody] SalesReportFilterDto filter)
    {
        try
        {
            var report = await _salesReportService.GenerateSalesReportAsync(filter);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório de vendas");
            return StatusCode(500, new { message = "Erro ao gerar relatório de vendas" });
        }
    }

    /// <summary>
    /// Exporta relatório de vendas em PDF ou Excel (POST - para chamadas programáticas)
    /// </summary>
    [HttpPost("sales/export")]
    public async Task<IActionResult> ExportSalesReport([FromBody] SalesReportFilterDto filter)
    {
        return await ExportSalesReportInternal(filter);
    }

    /// <summary>
    /// Exporta relatório de vendas em PDF ou Excel (GET - para download via browser)
    /// </summary>
    [HttpGet("sales/export")]
    public async Task<IActionResult> ExportSalesReportGet([FromQuery] SalesReportFilterDto filter)
    {
        return await ExportSalesReportInternal(filter);
    }

    private async Task<IActionResult> ExportSalesReportInternal(SalesReportFilterDto filter)
    {
        try
        {
            var report = await _salesReportService.GenerateSalesReportAsync(filter);

            if (filter.ExportFormat?.ToLower() == "pdf")
            {
                var pdfBytes = _pdfExportService.ExportSalesReportToPdf(report, filter);
                return File(pdfBytes, "application/pdf", $"relatorio-vendas-{DateTime.Now:yyyyMMdd}.pdf");
            }
            else
            {
                var excelBytes = _excelExportService.ExportSalesReportToExcel(report, filter);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                    $"relatorio-vendas-{DateTime.Now:yyyyMMdd}.xlsx");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao exportar relatório de vendas");
            return StatusCode(500, new { message = "Erro ao exportar relatório de vendas" });
        }
    }

    /// <summary>
    /// Gera mapa de calor de vendas por hora e dia da semana
    /// </summary>
    [HttpPost("sales/heatmap")]
    public async Task<ActionResult<SalesHeatmapReportDto>> GenerateSalesHeatmap([FromBody] SalesReportFilterDto filter)
    {
        try
        {
            var report = await _salesReportService.GenerateSalesHeatmapAsync(filter);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar mapa de calor de vendas");
            return StatusCode(500, new { message = "Erro ao gerar mapa de calor de vendas" });
        }
    }

    #endregion

    #region Financial Reports

    /// <summary>
    /// Gera relatório de fluxo de caixa
    /// </summary>
    [HttpPost("financial/cash-flow")]
    public async Task<ActionResult<CashFlowReportDto>> GenerateCashFlowReport([FromBody] FinancialReportFilterDto filter)
    {
        try
        {
            var report = await _financialReportService.GenerateCashFlowReportAsync(filter);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório de fluxo de caixa");
            return StatusCode(500, new { message = "Erro ao gerar relatório de fluxo de caixa" });
        }
    }

    /// <summary>
    /// Exporta relatório de fluxo de caixa (POST - para chamadas programáticas)
    /// </summary>
    [HttpPost("financial/cash-flow/export")]
    public async Task<IActionResult> ExportCashFlowReport([FromBody] FinancialReportFilterDto filter)
    {
        return await ExportCashFlowReportInternal(filter);
    }

    /// <summary>
    /// Exporta relatório de fluxo de caixa (GET - para download via browser)
    /// </summary>
    [HttpGet("financial/cash-flow/export")]
    public async Task<IActionResult> ExportCashFlowReportGet([FromQuery] FinancialReportFilterDto filter)
    {
        return await ExportCashFlowReportInternal(filter);
    }

    private async Task<IActionResult> ExportCashFlowReportInternal(FinancialReportFilterDto filter)
    {
        try
        {
            var report = await _financialReportService.GenerateCashFlowReportAsync(filter);

            if (filter.ExportFormat?.ToLower() == "pdf")
            {
                var pdfBytes = _pdfExportService.ExportCashFlowReportToPdf(report, filter);
                return File(pdfBytes, "application/pdf", $"fluxo-caixa-{DateTime.Now:yyyyMMdd}.pdf");
            }
            else
            {
                var excelBytes = _excelExportService.ExportCashFlowReportToExcel(report, filter);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                    $"fluxo-caixa-{DateTime.Now:yyyyMMdd}.xlsx");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao exportar relatório de fluxo de caixa");
            return StatusCode(500, new { message = "Erro ao exportar relatório" });
        }
    }

    /// <summary>
    /// Gera DRE (Demonstrativo de Resultados)
    /// </summary>
    [HttpPost("financial/profit-loss")]
    public async Task<ActionResult<ProfitLossReportDto>> GenerateProfitLossReport([FromBody] FinancialReportFilterDto filter)
    {
        try
        {
            var report = await _financialReportService.GenerateProfitLossReportAsync(filter);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar DRE");
            return StatusCode(500, new { message = "Erro ao gerar DRE" });
        }
    }

    /// <summary>
    /// Exporta DRE (POST - para chamadas programáticas)
    /// </summary>
    [HttpPost("financial/profit-loss/export")]
    public async Task<IActionResult> ExportProfitLossReport([FromBody] FinancialReportFilterDto filter)
    {
        return await ExportProfitLossReportInternal(filter);
    }

    /// <summary>
    /// Exporta DRE (GET - para download via browser)
    /// </summary>
    [HttpGet("financial/profit-loss/export")]
    public async Task<IActionResult> ExportProfitLossReportGet([FromQuery] FinancialReportFilterDto filter)
    {
        return await ExportProfitLossReportInternal(filter);
    }

    private async Task<IActionResult> ExportProfitLossReportInternal(FinancialReportFilterDto filter)
    {
        try
        {
            var report = await _financialReportService.GenerateProfitLossReportAsync(filter);

            if (filter.ExportFormat?.ToLower() == "pdf")
            {
                var pdfBytes = _pdfExportService.ExportProfitLossReportToPdf(report, filter);
                return File(pdfBytes, "application/pdf", $"dre-{DateTime.Now:yyyyMMdd}.pdf");
            }
            else
            {
                var excelBytes = _excelExportService.ExportProfitLossReportToExcel(report, filter);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                    $"dre-{DateTime.Now:yyyyMMdd}.xlsx");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao exportar DRE");
            return StatusCode(500, new { message = "Erro ao exportar DRE" });
        }
    }

    /// <summary>
    /// Gera balanço patrimonial
    /// </summary>
    [HttpPost("financial/balance-sheet")]
    public async Task<ActionResult<BalanceSheetReportDto>> GenerateBalanceSheetReport([FromBody] FinancialReportFilterDto filter)
    {
        try
        {
            var report = await _financialReportService.GenerateBalanceSheetReportAsync(filter);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar balanço patrimonial");
            return StatusCode(500, new { message = "Erro ao gerar balanço patrimonial" });
        }
    }

    #endregion

    #region Inventory Reports

    /// <summary>
    /// Gera relatório de níveis de estoque
    /// </summary>
    [HttpPost("inventory/stock-levels")]
    public async Task<ActionResult<StockLevelsReportDto>> GenerateStockLevelsReport([FromBody] InventoryReportFilterDto filter)
    {
        try
        {
            var report = await _inventoryReportService.GenerateStockLevelsReportAsync(filter);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório de estoque");
            return StatusCode(500, new { message = "Erro ao gerar relatório de estoque" });
        }
    }

    /// <summary>
    /// Gera relatório de Curva ABC (Pareto)
    /// </summary>
    [HttpPost("inventory/abc-curve")]
    public async Task<ActionResult<ABCCurveReportDto>> GenerateABCCurveReport([FromBody] InventoryReportFilterDto filter)
    {
        try
        {
            var report = await _inventoryReportService.GenerateABCCurveReportAsync(filter);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório de curva ABC");
            return StatusCode(500, new { message = "Erro ao gerar relatório de curva ABC" });
        }
    }

    /// <summary>
    /// Exporta relatório de níveis de estoque (POST - para chamadas programáticas)
    /// </summary>
    [HttpPost("inventory/stock-levels/export")]
    public async Task<IActionResult> ExportStockLevelsReport([FromBody] InventoryReportFilterDto filter)
    {
        return await ExportStockLevelsReportInternal(filter);
    }

    /// <summary>
    /// Exporta relatório de níveis de estoque (GET - para download via browser)
    /// </summary>
    [HttpGet("inventory/stock-levels/export")]
    public async Task<IActionResult> ExportStockLevelsReportGet([FromQuery] InventoryReportFilterDto filter)
    {
        return await ExportStockLevelsReportInternal(filter);
    }

    private async Task<IActionResult> ExportStockLevelsReportInternal(InventoryReportFilterDto filter)
    {
        try
        {
            var report = await _inventoryReportService.GenerateStockLevelsReportAsync(filter);

            if (filter.ExportFormat?.ToLower() == "pdf")
            {
                var pdfBytes = _pdfExportService.ExportStockLevelsReportToPdf(report, filter);
                return File(pdfBytes, "application/pdf", $"niveis-estoque-{DateTime.Now:yyyyMMdd}.pdf");
            }
            else
            {
                var excelBytes = _excelExportService.ExportStockLevelsReportToExcel(report, filter);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                    $"niveis-estoque-{DateTime.Now:yyyyMMdd}.xlsx");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao exportar relatório de estoque");
            return StatusCode(500, new { message = "Erro ao exportar relatório" });
        }
    }

    /// <summary>
    /// Gera relatório de movimentação de estoque
    /// </summary>
    [HttpPost("inventory/movements")]
    public async Task<ActionResult<StockMovementReportDto>> GenerateStockMovementReport([FromBody] InventoryReportFilterDto filter)
    {
        try
        {
            var report = await _inventoryReportService.GenerateStockMovementReportAsync(filter);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório de movimentações");
            return StatusCode(500, new { message = "Erro ao gerar relatório de movimentações" });
        }
    }

    /// <summary>
    /// Exporta relatório de movimentação de estoque (POST - para chamadas programáticas)
    /// </summary>
    [HttpPost("inventory/movements/export")]
    public async Task<IActionResult> ExportStockMovementReport([FromBody] InventoryReportFilterDto filter)
    {
        return await ExportStockMovementReportInternal(filter);
    }

    /// <summary>
    /// Exporta relatório de movimentação de estoque (GET - para download via browser)
    /// </summary>
    [HttpGet("inventory/movements/export")]
    public async Task<IActionResult> ExportStockMovementReportGet([FromQuery] InventoryReportFilterDto filter)
    {
        return await ExportStockMovementReportInternal(filter);
    }

    private async Task<IActionResult> ExportStockMovementReportInternal(InventoryReportFilterDto filter)
    {
        try
        {
            var report = await _inventoryReportService.GenerateStockMovementReportAsync(filter);
            var excelBytes = _excelExportService.ExportStockMovementReportToExcel(report, filter);
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                $"movimentacoes-estoque-{DateTime.Now:yyyyMMdd}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao exportar relatório de movimentações");
            return StatusCode(500, new { message = "Erro ao exportar relatório" });
        }
    }

    /// <summary>
    /// Gera relatório de avaliação de estoque
    /// </summary>
    [HttpPost("inventory/valuation")]
    public async Task<ActionResult<InventoryValuationReportDto>> GenerateInventoryValuationReport([FromBody] InventoryReportFilterDto filter)
    {
        try
        {
            var report = await _inventoryReportService.GenerateInventoryValuationReportAsync(filter);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório de avaliação de estoque");
            return StatusCode(500, new { message = "Erro ao gerar relatório de avaliação" });
        }
    }

    #endregion

    #region HR Reports

    /// <summary>
    /// Gera relatório de presença
    /// </summary>
    [HttpPost("hr/attendance")]
    public async Task<ActionResult<AttendanceReportDto>> GenerateAttendanceReport([FromBody] HRReportFilterDto filter)
    {
        try
        {
            var report = await _hrReportService.GenerateAttendanceReportAsync(filter);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório de presença");
            return StatusCode(500, new { message = "Erro ao gerar relatório de presença" });
        }
    }

    /// <summary>
    /// Gera relatório de turnover
    /// </summary>
    [HttpPost("hr/turnover")]
    public async Task<ActionResult<TurnoverReportDto>> GenerateTurnoverReport([FromBody] HRReportFilterDto filter)
    {
        try
        {
            var report = await _hrReportService.GenerateTurnoverReportAsync(filter);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório de turnover");
            return StatusCode(500, new { message = "Erro ao gerar relatório de turnover" });
        }
    }

    /// <summary>
    /// Gera relatório de headcount
    /// </summary>
    [HttpPost("hr/headcount")]
    public async Task<ActionResult<HeadcountReportDto>> GenerateHeadcountReport([FromBody] HRReportFilterDto filter)
    {
        try
        {
            var report = await _hrReportService.GenerateHeadcountReportAsync(filter);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório de headcount");
            return StatusCode(500, new { message = "Erro ao gerar relatório de headcount" });
        }
    }

    /// <summary>
    /// Exporta relatório de headcount (POST - para chamadas programáticas)
    /// </summary>
    [HttpPost("hr/headcount/export")]
    public async Task<IActionResult> ExportHeadcountReport([FromBody] HRReportFilterDto filter)
    {
        return await ExportHeadcountReportInternal(filter);
    }

    /// <summary>
    /// Exporta relatório de headcount (GET - para download via browser)
    /// </summary>
    [HttpGet("hr/headcount/export")]
    public async Task<IActionResult> ExportHeadcountReportGet([FromQuery] HRReportFilterDto filter)
    {
        return await ExportHeadcountReportInternal(filter);
    }

    private async Task<IActionResult> ExportHeadcountReportInternal(HRReportFilterDto filter)
    {
        try
        {
            var report = await _hrReportService.GenerateHeadcountReportAsync(filter);

            if (filter.ExportFormat?.ToLower() == "pdf")
            {
                var pdfBytes = _pdfExportService.ExportHeadcountReportToPdf(report, filter);
                return File(pdfBytes, "application/pdf", $"headcount-{DateTime.Now:yyyyMMdd}.pdf");
            }
            else
            {
                var excelBytes = _excelExportService.ExportHeadcountReportToExcel(report, filter);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                    $"headcount-{DateTime.Now:yyyyMMdd}.xlsx");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao exportar relatório de headcount");
            return StatusCode(500, new { message = "Erro ao exportar relatório" });
        }
    }

    #endregion
}
