using erp.Data;
using erp.Models.Payroll;
using erp.Models.TimeTracking;
using erp.Services.TimeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace erp.Services.Payroll;

public interface IPayrollService
{
    Task<IReadOnlyList<PayrollPeriod>> GetPeriodsAsync(int? year, PayrollPeriodStatus? status, CancellationToken cancellationToken = default);
    Task<PayrollPeriod?> GetPeriodAsync(int id, CancellationToken cancellationToken = default);
    Task<PayrollPeriod> CreatePeriodAsync(int month, int year, int createdById, CancellationToken cancellationToken = default);
    Task<PayrollPeriod> CalculatePeriodAsync(int periodId, int requestedById, CancellationToken cancellationToken = default);
    Task<PayrollPeriod> ApprovePeriodAsync(int periodId, int requestedById, string? notes, CancellationToken cancellationToken = default);
    Task<PayrollPeriod> MarkAsPaidAsync(int periodId, DateTime paymentDate, int requestedById, string? notes, CancellationToken cancellationToken = default);
    Task<PayrollSlip> GenerateSlipAsync(int periodId, int resultId, int requestedById, CancellationToken cancellationToken = default);
    Task<PayrollSlip?> GetSlipAsync(int periodId, int resultId, CancellationToken cancellationToken = default);
}

public class PayrollService : IPayrollService
{
    private readonly ApplicationDbContext _context;
    private readonly ITimeTrackingService _timeTrackingService;
    private readonly IPayrollCalculationService _calculationService;
    private readonly IPayrollSlipService _slipService;
    private readonly ILogger<PayrollService> _logger;

    public PayrollService(
        ApplicationDbContext context,
        ITimeTrackingService timeTrackingService,
        IPayrollCalculationService calculationService,
        IPayrollSlipService slipService,
        ILogger<PayrollService> logger)
    {
        _context = context;
        _timeTrackingService = timeTrackingService;
        _calculationService = calculationService;
        _slipService = slipService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PayrollPeriod>> GetPeriodsAsync(int? year, PayrollPeriodStatus? status, CancellationToken cancellationToken = default)
    {
        var query = _context.PayrollPeriods
            .AsNoTracking()
            .Include(p => p.Results)
            .OrderByDescending(p => p.ReferenceYear)
            .ThenByDescending(p => p.ReferenceMonth)
            .AsQueryable();

        if (year.HasValue)
        {
            query = query.Where(p => p.ReferenceYear == year.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public Task<PayrollPeriod?> GetPeriodAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.PayrollPeriods
            .AsNoTracking()
            .Include(p => p.Results)
                .ThenInclude(r => r.Components)
            .Include(p => p.Results)
                .ThenInclude(r => r.Slip)
                    .ThenInclude(s => s.GeneratedBy)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<PayrollPeriod> CreatePeriodAsync(int month, int year, int createdById, CancellationToken cancellationToken = default)
    {
        var existing = await _context.PayrollPeriods
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ReferenceMonth == month && p.ReferenceYear == year, cancellationToken);

        if (existing != null)
        {
            return existing;
        }

        return await _timeTrackingService.CreatePeriodAsync(month, year, createdById, cancellationToken);
    }

    public Task<PayrollPeriod> CalculatePeriodAsync(int periodId, int requestedById, CancellationToken cancellationToken = default)
    {
        return _calculationService.CalculateAsync(periodId, requestedById, cancellationToken);
    }

    public async Task<PayrollPeriod> ApprovePeriodAsync(int periodId, int requestedById, string? notes, CancellationToken cancellationToken = default)
    {
        var period = await _context.PayrollPeriods
            .FirstOrDefaultAsync(p => p.Id == periodId, cancellationToken)
            ?? throw new KeyNotFoundException("Período não encontrado.");

        if (period.Status is not PayrollPeriodStatus.Calculated)
        {
            throw new InvalidOperationException("A folha precisa estar calculada para ser aprovada.");
        }

        var now = DateTime.UtcNow;
        period.Status = PayrollPeriodStatus.Approved;
        period.ApprovedAt = now;
        period.ApprovedById = requestedById;
        period.UpdatedAt = now;
        period.UpdatedById = requestedById;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            period.Notes = notes.Trim();
        }

        await _context.SaveChangesAsync(cancellationToken);
        return period;
    }

    public async Task<PayrollPeriod> MarkAsPaidAsync(int periodId, DateTime paymentDate, int requestedById, string? notes, CancellationToken cancellationToken = default)
    {
        var period = await _context.PayrollPeriods
            .FirstOrDefaultAsync(p => p.Id == periodId, cancellationToken)
            ?? throw new KeyNotFoundException("Período não encontrado.");

        if (period.Status is not PayrollPeriodStatus.Approved)
        {
            throw new InvalidOperationException("A folha precisa estar aprovada antes de registrar o pagamento.");
        }

        var normalizedPaymentDate = DateTime.SpecifyKind(paymentDate, DateTimeKind.Utc);

        period.Status = PayrollPeriodStatus.Paid;
        period.PaidAt = normalizedPaymentDate;
        period.PaidById = requestedById;
        period.UpdatedAt = DateTime.UtcNow;
        period.UpdatedById = requestedById;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            period.Notes = notes.Trim();
        }

        await _context.SaveChangesAsync(cancellationToken);
        return period;
    }

    public async Task<PayrollSlip> GenerateSlipAsync(int periodId, int resultId, int requestedById, CancellationToken cancellationToken = default)
    {
        var result = await _context.PayrollResults
            .Include(r => r.PayrollPeriod)
            .Include(r => r.Components)
            .Include(r => r.Slip)
                .ThenInclude(s => s.GeneratedBy)
            .FirstOrDefaultAsync(r => r.Id == resultId && r.PayrollPeriodId == periodId, cancellationToken)
            ?? throw new KeyNotFoundException("Resultado da folha não encontrado.");

        if (result.PayrollPeriod?.Status is PayrollPeriodStatus.Draft)
        {
            throw new InvalidOperationException("Calcule a folha antes de gerar os holerites.");
        }

        return await _slipService.GenerateAsync(result, requestedById, cancellationToken);
    }

    public async Task<PayrollSlip?> GetSlipAsync(int periodId, int resultId, CancellationToken cancellationToken = default)
    {
        return await _context.PayrollSlips
            .Include(s => s.PayrollResult)
                .ThenInclude(r => r.PayrollPeriod)
            .Include(s => s.GeneratedBy)
            .FirstOrDefaultAsync(s => s.PayrollResultId == resultId && s.PayrollResult.PayrollPeriodId == periodId, cancellationToken);
    }
}
