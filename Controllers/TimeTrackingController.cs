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
[Route("api/time-tracking")]
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

    [HttpGet("periods")]
    public async Task<ActionResult<IEnumerable<PayrollPeriodSummaryDto>>> GetPeriods(
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        var periods = await _timeTrackingService.GetPeriodsAsync(year, cancellationToken);
        var payload = _mapper.ToSummaryDto(periods);
        return Ok(payload);
    }

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
