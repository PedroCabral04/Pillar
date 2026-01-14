using System.IO;
using erp.DTOs.Payroll;
using erp.Mappings;
using erp.Models.Payroll;
using erp.Models.TimeTracking;
using erp.Services.Payroll;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace erp.Controllers;

[ApiController]
[Route("api/payroll")]
[Authorize(Roles = "Administrador,Gerente,RH")]
[ResponseCache(NoStore = true)]
public class PayrollController : ControllerBase
{
    private readonly IPayrollService _payrollService;
    private readonly IPayrollSlipService _slipService;
    private readonly PayrollMapper _mapper;

    public PayrollController(IPayrollService payrollService, IPayrollSlipService slipService, PayrollMapper mapper)
    {
        _payrollService = payrollService;
        _slipService = slipService;
        _mapper = mapper;
    }

    /// <summary>
    /// Retorna os períodos de folha registrados no sistema, com filtros opcionais.
    /// </summary>
    /// <param name="year">Ano de referência (opcional).</param>
    /// <param name="status">Filtro por status do período (opcional).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de <see cref="PayrollPeriodListDto"/>.</returns>
    [HttpGet("periods")]
    public async Task<ActionResult<IEnumerable<PayrollPeriodListDto>>> GetPeriods(
        [FromQuery] int? year,
        [FromQuery] PayrollPeriodStatus? status,
        CancellationToken cancellationToken)
    {
        var periods = await _payrollService.GetPeriodsAsync(year, status, cancellationToken);
        var payload = _mapper.ToListDto(periods).ToList();

        for (var i = 0; i < payload.Count; i++)
        {
            payload[i].TotalEmployees = periods[i].Results?.Count ?? 0;
        }

        return Ok(payload);
    }

    /// <summary>
    /// Retorna os detalhes de um período de folha específico.
    /// </summary>
    /// <param name="id">Identificador do período.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Objeto <see cref="PayrollPeriodDetailDto"/> ou 404 se não existir.</returns>
    [HttpGet("periods/{id:int}")]
    public async Task<ActionResult<PayrollPeriodDetailDto>> GetPeriod(int id, CancellationToken cancellationToken)
    {
        var period = await _payrollService.GetPeriodAsync(id, cancellationToken);
        if (period == null)
        {
            return NotFound();
        }

        var dto = _mapper.ToDetailDto(period);
        EnrichPeriodDto(dto, period);
        return Ok(dto);
    }

    /// <summary>
    /// Cria um novo período de folha para o mês/ano referenciados.
    /// </summary>
    /// <param name="request">Dados de criação do período.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Objeto criado <see cref="PayrollPeriodDetailDto"/> com código 201.</returns>
    [HttpPost("periods")]
    public async Task<ActionResult<PayrollPeriodDetailDto>> CreatePeriod([FromBody] CreatePayrollPeriodRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var period = await _payrollService.CreatePeriodAsync(request.ReferenceMonth, request.ReferenceYear, userId.Value, cancellationToken);
        var response = _mapper.ToDetailDto(period);
        EnrichPeriodDto(response, period);
        return CreatedAtAction(nameof(GetPeriod), new { id = response.Id }, response);
    }

    /// <summary>
    /// Executa o cálculo da folha para o período especificado.
    /// </summary>
    /// <param name="id">Identificador do período.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Detalhes do período após o cálculo.</returns>
    [HttpPost("periods/{id:int}/calculate")]
    public async Task<ActionResult<PayrollPeriodDetailDto>> Calculate(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var period = await _payrollService.CalculatePeriodAsync(id, userId.Value, cancellationToken);
        var dto = _mapper.ToDetailDto(period);
        EnrichPeriodDto(dto, period);
        return Ok(dto);
    }

    /// <summary>
    /// Aprova o período de folha, registrando notas opcionais.
    /// </summary>
    /// <param name="id">Identificador do período.</param>
    /// <param name="request">Dados da aprovação (ex.: notas).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Detalhes do período após aprovação.</returns>
    [HttpPost("periods/{id:int}/approve")]
    public async Task<ActionResult<PayrollPeriodDetailDto>> Approve(
        int id,
        [FromBody] ApprovePayrollPeriodRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var period = await _payrollService.ApprovePeriodAsync(id, userId.Value, request?.Notes, cancellationToken);
        var dto = _mapper.ToDetailDto(period);
        EnrichPeriodDto(dto, period);
        return Ok(dto);
    }

    /// <summary>
    /// Marca o período como pago e registra a data de pagamento.
    /// </summary>
    /// <param name="id">Identificador do período.</param>
    /// <param name="request">Dados de pagamento (data e notas).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Detalhes do período após a marcação como pago.</returns>
    [HttpPost("periods/{id:int}/pay")]
    public async Task<ActionResult<PayrollPeriodDetailDto>> Pay(
        int id,
        [FromBody] PayPayrollPeriodRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var period = await _payrollService.MarkAsPaidAsync(id, request.PaymentDate, userId.Value, request.Notes, cancellationToken);
        var dto = _mapper.ToDetailDto(period);
        EnrichPeriodDto(dto, period);
        return Ok(dto);
    }

    /// <summary>
    /// Gera o holerite (slip) para um resultado de período específico.
    /// </summary>
    /// <param name="periodId">Identificador do período.</param>
    /// <param name="resultId">Identificador do resultado do funcionário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>DTO do holerite gerado.</returns>
    [HttpPost("periods/{periodId:int}/results/{resultId:int}/slip")]
    public async Task<ActionResult<PayrollSlipDto>> GenerateSlip(int periodId, int resultId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var slip = await _payrollService.GenerateSlipAsync(periodId, resultId, userId.Value, cancellationToken);
        return Ok(CreateSlipDto(slip));
    }

    /// <summary>
    /// Faz o download do arquivo do holerite (PDF/arquivo gerado) para o resultado especificado.
    /// </summary>
    /// <param name="periodId">Identificador do período.</param>
    /// <param name="resultId">Identificador do resultado do funcionário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Arquivo do tipo apropriado ou 404 se não existir.</returns>
    [HttpGet("periods/{periodId:int}/results/{resultId:int}/slip")]
    public async Task<IActionResult> DownloadSlip(int periodId, int resultId, CancellationToken cancellationToken)
    {
        var slip = await _payrollService.GetSlipAsync(periodId, resultId, cancellationToken);
        if (slip == null)
        {
            return NotFound();
        }

        var bytes = await _slipService.ReadAsync(slip, cancellationToken);
        if (bytes.Length == 0)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var regenerated = await _payrollService.GenerateSlipAsync(periodId, resultId, userId.Value, cancellationToken);
            bytes = await _slipService.ReadAsync(regenerated, cancellationToken);
        }

        return File(bytes, slip.ContentType, Path.GetFileName(slip.FilePath));
    }

    private void EnrichPeriodDto(PayrollPeriodDetailDto dto, PayrollPeriod entity)
    {
        dto.TotalEmployees = entity.Results?.Count ?? 0;

    if (dto.Results == null || dto.Results.Count == 0 || entity.Results == null || entity.Results.Count == 0)
        {
            return;
        }

        var entityLookup = entity.Results.ToDictionary(r => r.Id);
        foreach (var resultDto in dto.Results)
        {
            if (entityLookup.TryGetValue(resultDto.Id, out var resultEntity))
            {
                if (resultEntity.Slip != null)
                {
                    resultDto.Slip = CreateSlipDto(resultEntity.Slip);
                }

                if (resultDto.Components?.Count > 1)
                {
                    resultDto.Components = resultDto.Components
                        .OrderBy(c => c.Sequence)
                        .ToList();
                }
            }
        }
    }

    private PayrollSlipDto CreateSlipDto(PayrollSlip slip)
    {
        var relative = slip.FilePath?.Replace("\\", "/") ?? string.Empty;
        if (!string.IsNullOrEmpty(relative) && !relative.StartsWith('/'))
        {
            relative = "/" + relative;
        }

        return new PayrollSlipDto
        {
            Id = slip.Id,
            FileUrl = relative,
            FileHash = slip.FileHash,
            ContentType = slip.ContentType,
            FileSize = slip.FileSize,
            GeneratedAt = slip.GeneratedAt,
            GeneratedBy = slip.GeneratedBy?.FullName ?? slip.GeneratedBy?.UserName ?? string.Empty
        };
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (claim == null)
        {
            return null;
        }

        return int.TryParse(claim.Value, out var userId) ? userId : null;
    }
}
