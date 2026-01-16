using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using erp.DTOs.ServiceOrders;
using erp.Services.ServiceOrders;
using erp.Services.Tenancy;
using erp.Services.Reports;
using System.Security.Claims;

namespace erp.Controllers;

/// <summary>
/// Controller para gerenciamento de Ordens de Serviço
/// </summary>
[Authorize]
[ApiController]
[Route("api/ordens-servico")]
public class ServiceOrdersController : ControllerBase
{
    private readonly IServiceOrderService _serviceOrderService;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly IPdfExportService _pdfExportService;
    private readonly ILogger<ServiceOrdersController> _logger;

    public ServiceOrdersController(
        IServiceOrderService serviceOrderService,
        ITenantContextAccessor tenantContextAccessor,
        IPdfExportService pdfExportService,
        ILogger<ServiceOrdersController> logger)
    {
        _serviceOrderService = serviceOrderService;
        _tenantContextAccessor = tenantContextAccessor;
        _pdfExportService = pdfExportService;
        _logger = logger;
    }

    private int? CurrentTenantId => _tenantContextAccessor.Current?.TenantId;
    private int? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value != null
        ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value)
        : null;

    /// <summary>
    /// Cria uma nova ordem de serviço
    /// </summary>
    /// <param name="dto">Dados da ordem de serviço</param>
    /// <returns>Ordem de serviço criada</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ServiceOrderDto>> CreateServiceOrder([FromBody] CreateServiceOrderDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!CurrentUserId.HasValue)
                return Unauthorized(new { message = "Usuário não autenticado" });

            if (!CurrentTenantId.HasValue)
                return BadRequest(new { message = "Tenant não identificado" });

            var order = await _serviceOrderService.CreateAsync(dto, CurrentUserId.Value, CurrentTenantId.Value);
            return CreatedAtAction(nameof(GetServiceOrderById), new { id = order.Id }, order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar ordem de serviço");
            return StatusCode(500, new { message = "Erro ao criar ordem de serviço", error = ex.Message });
        }
    }

    /// <summary>
    /// Busca uma ordem de serviço por ID
    /// </summary>
    /// <param name="id">ID da ordem de serviço</param>
    /// <returns>Dados completos da ordem</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ServiceOrderDto>> GetServiceOrderById(int id)
    {
        try
        {
            var order = await _serviceOrderService.GetByIdAsync(id);
            if (order == null)
                return NotFound(new { message = $"Ordem de serviço com ID {id} não encontrada" });

            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar ordem de serviço {OrderId}", id);
            return StatusCode(500, new { message = "Erro ao buscar ordem de serviço", error = ex.Message });
        }
    }

    /// <summary>
    /// Lista ordens de serviço com filtros e paginação
    /// </summary>
    /// <param name="search">Termo de busca (número, cliente, aparelho, serial)</param>
    /// <param name="status">Filtro por status</param>
    /// <param name="startDate">Data inicial</param>
    /// <param name="endDate">Data final</param>
    /// <param name="customerId">Filtro por cliente</param>
    /// <param name="page">Página atual (padrão: 1)</param>
    /// <param name="pageSize">Itens por página (padrão: 20)</param>
    /// <returns>Lista paginada de ordens de serviço</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaginatedServiceOrdersResponse>> SearchServiceOrders(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int? customerId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var (items, total) = await _serviceOrderService.SearchAsync(
                search, status, startDate, endDate, customerId, page, pageSize);

            return Ok(new PaginatedServiceOrdersResponse
            {
                Items = items,
                Total = total,
                Page = page,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar ordens de serviço");
            return StatusCode(500, new { message = "Erro ao buscar ordens de serviço", error = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza uma ordem de serviço existente
    /// </summary>
    /// <param name="id">ID da ordem de serviço</param>
    /// <param name="dto">Dados para atualização</param>
    /// <returns>Ordem de serviço atualizada</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ServiceOrderDto>> UpdateServiceOrder(
        int id,
        [FromBody] UpdateServiceOrderDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var order = await _serviceOrderService.UpdateAsync(id, dto);
            return Ok(order);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar ordem de serviço {OrderId}", id);
            return StatusCode(500, new { message = "Erro ao atualizar ordem de serviço", error = ex.Message });
        }
    }

    /// <summary>
    /// Cancela uma ordem de serviço
    /// </summary>
    /// <param name="id">ID da ordem de serviço</param>
    /// <returns>Confirmação do cancelamento</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteServiceOrder(int id)
    {
        try
        {
            var result = await _serviceOrderService.DeleteAsync(id);
            if (!result)
                return NotFound(new { message = $"Ordem de serviço com ID {id} não encontrada" });

            return Ok(new { message = "Ordem de serviço cancelada com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao cancelar ordem de serviço {OrderId}", id);
            return StatusCode(500, new { message = "Erro ao cancelar ordem de serviço", error = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza o status de uma ordem de serviço
    /// </summary>
    /// <param name="id">ID da ordem de serviço</param>
    /// <param name="dto">Novo status e notas opcionais</param>
    /// <returns>Ordem de serviço atualizada</returns>
    [HttpPost("{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ServiceOrderDto>> UpdateStatus(
        int id,
        [FromBody] UpdateStatusDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var order = await _serviceOrderService.UpdateStatusAsync(id, dto.Status, dto.Notes);
            return Ok(order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar status da ordem de serviço {OrderId}", id);
            return StatusCode(500, new { message = "Erro ao atualizar status", error = ex.Message });
        }
    }

    /// <summary>
    /// Marca uma ordem como concluída
    /// </summary>
    /// <param name="id">ID da ordem de serviço</param>
    /// <returns>Ordem de serviço atualizada</returns>
    [HttpPost("{id:int}/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ServiceOrderDto>> CompleteServiceOrder(int id)
    {
        try
        {
            var order = await _serviceOrderService.CompleteAsync(id);
            return Ok(order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao concluir ordem de serviço {OrderId}", id);
            return StatusCode(500, new { message = "Erro ao concluir ordem de serviço", error = ex.Message });
        }
    }

    /// <summary>
    /// Marca uma ordem como entregue
    /// </summary>
    /// <param name="id">ID da ordem de serviço</param>
    /// <returns>Ordem de serviço atualizada</returns>
    [HttpPost("{id:int}/deliver")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ServiceOrderDto>> DeliverServiceOrder(int id)
    {
        try
        {
            var order = await _serviceOrderService.DeliverAsync(id);
            return Ok(order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao entregar ordem de serviço {OrderId}", id);
            return StatusCode(500, new { message = "Erro ao entregar ordem de serviço", error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém resumo estatístico por status
    /// </summary>
    /// <returns>Lista com contagem e valores por status</returns>
    [HttpGet("summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<ServiceOrderStatusSummaryDto>>> GetStatusSummary()
    {
        try
        {
            var summary = await _serviceOrderService.GetStatusSummaryAsync();
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter resumo de status");
            return StatusCode(500, new { message = "Erro ao obter resumo", error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém o total de receitas de serviços em um período
    /// </summary>
    /// <param name="startDate">Data inicial</param>
    /// <param name="endDate">Data final</param>
    /// <returns>Total de receitas</returns>
    [HttpGet("revenue")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<decimal>> GetRevenue(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            var revenue = await _serviceOrderService.GetTotalRevenueAsync(startDate, endDate);
            return Ok(revenue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao calcular receitas");
            return StatusCode(500, new { message = "Erro ao calcular receitas", error = ex.Message });
        }
    }

    /// <summary>
    /// Exporta ordem de serviço para PDF
    /// </summary>
    /// <param name="id">ID da ordem de serviço</param>
    /// <returns>Arquivo PDF</returns>
    [HttpGet("{id:int}/export/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportToPdf(int id)
    {
        try
        {
            var order = await _serviceOrderService.GetByIdAsync(id);
            if (order == null)
                return NotFound(new { message = $"Ordem de serviço com ID {id} não encontrada" });

            var pdf = _pdfExportService.ExportServiceOrderToPdf(order);
            return File(pdf, "application/pdf", $"OS_{order.OrderNumber}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar PDF da ordem de serviço {OrderId}", id);
            return StatusCode(500, new { message = "Erro ao gerar PDF", error = ex.Message });
        }
    }

    /// <summary>
    /// Retorna HTML para impressão da ordem de serviço
    /// </summary>
    /// <param name="id">ID da ordem de serviço</param>
    /// <returns>HTML formatado para impressão</returns>
    [HttpGet("{id:int}/print")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PrintServiceOrder(int id)
    {
        try
        {
            var order = await _serviceOrderService.GetByIdAsync(id);
            if (order == null)
                return NotFound(new { message = $"Ordem de serviço com ID {id} não encontrada" });

            var html = GeneratePrintHtml(order);
            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar HTML de impressão da ordem {OrderId}", id);
            return StatusCode(500, new { message = "Erro ao gerar HTML de impressão", error = ex.Message });
        }
    }

    private static string GeneratePrintHtml(ServiceOrderDto order)
    {
        var statusColors = new Dictionary<string, string>
        {
            ["Open"] = "#6c757d",
            ["InProgress"] = "#0d6efd",
            ["WaitingCustomer"] = "#fd7e14",
            ["WaitingParts"] = "#ffc107",
            ["Completed"] = "#198754",
            ["Delivered"] = "#0f5132",
            ["Cancelled"] = "#dc3545"
        };

        var statusColor = statusColors.GetValueOrDefault(order.Status, "#6c757d");

        var itemsHtml = string.Join("\n", order.Items.Select(item =>
            $@"        <tr>
            <td>{item.Description}</td>
            <td>{item.ServiceType ?? "-"}</td>
            <td class=""text-right"">{item.Price:F2}</td>
        </tr>"));

        var customerInfo = order.Customer != null
            ? $@"{order.Customer.Name}<br>
            {(string.IsNullOrEmpty(order.Customer.Document) ? "" : $"Doc: {order.Customer.Document}<br>")}
            {(string.IsNullOrEmpty(order.Customer.Mobile) ? "" : $"Tel: {order.Customer.Mobile}<br>")}"
            : "Cliente não informado";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title>OS {order.OrderNumber}</title>
    <style>
        body {{ font-family: Arial, sans-serif; font-size: 12px; line-height: 1.4; color: #333; }}
        .container {{ max-width: 800px; margin: 0 auto; padding: 20px; }}
        .header {{ display: flex; justify-content: space-between; align-items: center; border-bottom: 2px solid #0066cc; padding-bottom: 10px; margin-bottom: 20px; }}
        .title {{ font-size: 24px; font-weight: bold; color: #0066cc; }}
        .order-number {{ font-size: 20px; font-weight: bold; }}
        .status {{ background: {statusColor}; color: white; padding: 4px 12px; border-radius: 4px; font-weight: bold; }}
        .section {{ margin-bottom: 15px; }}
        .section-title {{ font-weight: bold; color: #0066cc; margin-bottom: 5px; }}
        .grid {{ display: grid; grid-template-columns: 1fr 1fr; gap: 20px; }}
        .device-info {{ background: #e3f2fd; padding: 10px; border: 1px solid #90caf9; }}
        .problem-box {{ background: #fff3e0; border-left: 4px solid #ff9800; padding: 10px; margin: 10px 0; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 10px; }}
        th {{ background: #0066cc; color: white; padding: 8px; text-align: left; }}
        td {{ padding: 8px; border-bottom: 1px solid #ddd; }}
        .text-right {{ text-align: right; }}
        .totals {{ background: #0066cc; color: white; padding: 12px; width: 220px; margin-left: auto; }}
        .totals-row {{ display: flex; justify-content: space-between; }}
        .totals-total {{ font-size: 16px; font-weight: bold; margin-top: 8px; padding-top: 8px; border-top: 1px solid white; }}
        .signatures {{ display: grid; grid-template-columns: 1fr 1fr; gap: 40px; margin-top: 40px; }}
        .signature {{ text-align: center; }}
        .signature-line {{ border-bottom: 1px solid black; height: 50px; }}
        .footer {{ margin-top: 30px; padding-top: 10px; border-top: 1px solid #ddd; font-size: 10px; color: #666; text-align: center; }}
        @media print {{ body {{ -webkit-print-color-adjust: exact; print-color-adjust: exact; }} }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <div>
                <div class=""title"">PILLAR ERP</div>
                <div>Assistência Técnica</div>
            </div>
            <div style=""text-align: right;"">
                <div>ORDEM DE SERVIÇO</div>
                <div class=""order-number"">{order.OrderNumber}</div>
                <div class=""status"">{order.StatusDisplay}</div>
            </div>
        </div>

        <div class=""grid section"">
            <div>
                <div class=""section-title"">DADOS DA ORDEM</div>
                Data Entrada: {order.EntryDate:dd/MM/yyyy}<br>
                {(order.EstimatedCompletionDate.HasValue ? $"Previsão: {order.EstimatedCompletionDate.Value:dd/MM/yyyy}<br>" : "")}
                {(order.ActualCompletionDate.HasValue ? $"Conclusão: {order.ActualCompletionDate.Value:dd/MM/yyyy}<br>" : "")}
                {(!string.IsNullOrEmpty(order.WarrantyType) ? $"Garantia: {order.WarrantyType}" : "")}
            </div>
            <div>
                <div class=""section-title"">DADOS DO CLIENTE</div>
                {customerInfo}
            </div>
        </div>

        {(!string.IsNullOrEmpty(order.DeviceBrand) || !string.IsNullOrEmpty(order.DeviceModel) ? $@"
        <div class=""device-info section"">
            <div class=""section-title"">APARELHO</div>
            {(!string.IsNullOrEmpty(order.DeviceBrand) ? $"Marca: {order.DeviceBrand}" : "")} {(!string.IsNullOrEmpty(order.DeviceModel) ? $"Modelo: {order.DeviceModel}" : "")}<br>
            {(!string.IsNullOrEmpty(order.DeviceType) ? $"Tipo: {order.DeviceType}" : "")} {(!string.IsNullOrEmpty(order.SerialNumber) ? $"Serial: {order.SerialNumber}" : "")}
        </div>" : "")}

        {(!string.IsNullOrWhiteSpace(order.ProblemDescription) ? $@"
        <div class=""problem-box"">
            <div class=""section-title"">PROBLEMA RELATADO</div>
            {order.ProblemDescription}
        </div>" : "")}

        {(order.Items.Any() ? $@"
        <div class=""section"">
            <div class=""section-title"">SERVIÇOS REALIZADOS ({order.Items.Count})</div>
            <table>
                <tr>
                    <th>Descrição</th>
                    <th>Tipo</th>
                    <th class=""text-right"">Valor</th>
                </tr>
{itemsHtml}
            </table>
        </div>" : "")}

        {(!string.IsNullOrWhiteSpace(order.TechnicalNotes) ? $@"
        <div class=""section"" style=""background: #f5f5f5; padding: 8px;"">
            <div class=""section-title"">NOTAS TÉCNICAS</div>
            {order.TechnicalNotes}
        </div>" : "")}

        {(!string.IsNullOrWhiteSpace(order.CustomerNotes) ? $@"
        <div class=""section"" style=""background: #e8f5e9; padding: 8px;"">
            <div class=""section-title"">OBSERVAÇÕES</div>
            {order.CustomerNotes}
        </div>" : "")}

        <div class=""totals"">
            <div class=""totals-row"">
                <span>Total dos Serviços</span>
                <span>{order.TotalAmount:F2}</span>
            </div>
            {(order.DiscountAmount > 0 ? $@"
            <div class=""totals-row"" style=""margin-top: 3px;"">
                <span>Desconto</span>
                <span>- {order.DiscountAmount:F2}</span>
            </div>" : "")}
            <div class=""totals-row totals-total"">
                <span>TOTAL</span>
                <span>{order.NetAmount:F2}</span>
            </div>
        </div>

        <div class=""signatures"">
            <div class=""signature"">
                <div class=""signature-line""></div>
                <div>Assinatura do Técnico</div>
            </div>
            <div class=""signature"">
                <div class=""signature-line""></div>
                <div>Assinatura do Cliente</div>
            </div>
        </div>

        <div class=""footer"">
            Documento gerado em {DateTime.Now:dd/MM/yyyy HH:mm} - Pillar ERP - Ordem de Serviço
        </div>
    </div>
</body>
</html>";
    }
}
