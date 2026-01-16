using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using erp.DTOs.TimeTracking;
using erp.Mappings;
using erp.Services.TimeTracking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace erp.Controllers;

[ApiController]
[Route("api/apontamento")]
[Authorize(Roles = "Administrador,Gerente")]
[ResponseCache(NoStore = true)]
public class TimeTrackingController : ControllerBase
{
    private readonly ITimeTrackingService _timeTrackingService;
    private readonly TimeTrackingMapper _mapper;

    public TimeTrackingController(
        ITimeTrackingService timeTrackingService,
        TimeTrackingMapper mapper)
    {
        _timeTrackingService = timeTrackingService;
        _mapper = mapper;
    }

    /// <summary>
    /// Recupera uma lista de resumos dos períodos de folha (payroll).
    /// </summary>
    /// <param name="year">Opcional. Filtra os períodos pelo ano de referência.</param>
    /// <param name="cancellationToken">Token para cancelamento da requisição.</param>
    /// <returns>Lista de <see cref="PayrollPeriodSummaryDto"/> com resumo dos períodos encontrados.</returns>
    /// <response code="200">Retorna a lista de períodos (mesmo que vazia).</response>
    /// <response code="401">Usuário não autorizado a acessar o recurso.</response>
    [HttpGet("periods")]
    public async Task<ActionResult<IEnumerable<PayrollPeriodSummaryDto>>> GetPeriods(
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        var periods = await _timeTrackingService.GetPeriodsAsync(year, cancellationToken);
        var payload = _mapper.ToSummaryDto(periods);
        return Ok(payload);
    }

    /// <summary>
    /// Recupera o detalhe de um período de folha por seu identificador.
    /// </summary>
    /// <param name="id">Identificador do período.</param>
    /// <param name="cancellationToken">Token para cancelamento da requisição.</param>
    /// <returns>Detalhes do período como <see cref="PayrollPeriodDetailDto"/>.</returns>
    /// <response code="200">Retorna o período solicitado.</response>
    /// <response code="404">Período não encontrado.</response>
    [HttpGet("periods/{id:int}")]
    public async Task<ActionResult<PayrollPeriodDetailDto>> GetPeriodById(
        int id,
        CancellationToken cancellationToken)
    {
        var period = await _timeTrackingService.GetPeriodAsync(id, cancellationToken);
        if (period == null)
        {
            return NotFound();
        }

        return Ok(_mapper.ToDetailDto(period));
    }

    /// <summary>
    /// Recupera o detalhe de um período de folha baseado em mês e ano de referência.
    /// </summary>
    /// <param name="month">Mês de referência (1-12).</param>
    /// <param name="year">Ano de referência (ex: 2025).</param>
    /// <param name="cancellationToken">Token para cancelamento da requisição.</param>
    /// <returns>Detalhes do período correspondente, se existir.</returns>
    /// <response code="200">Retorna o período correspondente ao mês/ano.</response>
    /// <response code="404">Período não encontrado para a referência informada.</response>
    [HttpGet("periods/by-reference")]
    public async Task<ActionResult<PayrollPeriodDetailDto>> GetPeriodByReference(
        [FromQuery] int month,
        [FromQuery] int year,
        CancellationToken cancellationToken)
    {
        var period = await _timeTrackingService.GetPeriodByReferenceAsync(month, year, cancellationToken);
        if (period == null)
        {
            return NotFound();
        }

        return Ok(_mapper.ToDetailDto(period));
    }

    /// <summary>
    /// Cria um novo período de folha para um mês/ano de referência.
    /// </summary>
    /// <param name="dto">Objeto com dados necessários para criação (<see cref="CreatePayrollPeriodDto"/>).</param>
    /// <param name="cancellationToken">Token para cancelamento da requisição.</param>
    /// <returns>O período criado com detalhes.</returns>
    /// <response code="201">Período criado com sucesso. Retorna o recurso criado.</response>
    /// <response code="400">Dados inválidos na requisição.</response>
    /// <response code="409">Conflito: já existe um período com a mesma referência.</response>
    [HttpPost("periods")]
    public async Task<ActionResult<PayrollPeriodDetailDto>> CreatePeriod(
        [FromBody] CreatePayrollPeriodDto dto,
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

        try
        {
            var period = await _timeTrackingService.CreatePeriodAsync(dto.ReferenceMonth, dto.ReferenceYear, userId.Value, cancellationToken);
            var response = _mapper.ToDetailDto(period);
            return CreatedAtAction(nameof(GetPeriodById), new { id = response.Id }, response);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza um lançamento (entry) de folha existente.
    /// </summary>
    /// <param name="entryId">Identificador do lançamento a ser atualizado.</param>
    /// <param name="dto">Objeto com os campos editáveis (<see cref="UpdatePayrollEntryDto"/>).</param>
    /// <param name="cancellationToken">Token para cancelamento da requisição.</param>
    /// <returns>O lançamento atualizado como <see cref="PayrollEntryDto"/>.</returns>
    /// <response code="200">Retorna o lançamento atualizado.</response>
    /// <response code="400">Dados inválidos na requisição.</response>
    /// <response code="404">Lançamento não encontrado.</response>
    [HttpPut("entries/{entryId:int}")]
    public async Task<ActionResult<PayrollEntryDto>> UpdateEntry(
        int entryId,
        [FromBody] UpdatePayrollEntryDto dto,
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

        try
        {
            var entry = await _timeTrackingService.UpdateEntryAsync(entryId, dto.Faltas, dto.Abonos, dto.HorasExtras, dto.Atrasos, dto.Observacoes, userId.Value, cancellationToken);
            return Ok(_mapper.ToEntryDto(entry));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Adiciona um colaborador ao período de folha.
    /// </summary>
    [HttpPost("periods/{periodId:int}/entries")]
    public async Task<ActionResult<PayrollEntryDto>> AddEntry(
        int periodId,
        [FromBody] int employeeId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        try
        {
            var entry = await _timeTrackingService.AddEntryAsync(periodId, employeeId, cancellationToken);
            return Ok(_mapper.ToEntryDto(entry));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza múltiplos lançamentos de uma vez.
    /// </summary>
    [HttpPut("entries/bulk")]
    public async Task<IActionResult> BulkUpdateEntries(
        [FromBody] List<BulkUpdatePayrollEntryDto> entries,
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

        await _timeTrackingService.UpdateEntriesAsync(entries, userId.Value, cancellationToken);
        return NoContent();
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null)
        {
            return null;
        }

        return int.TryParse(claim.Value, out var userId) ? userId : null;
    }
}
