using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using erp.Data;
using erp.DTOs.TimeTracking;
using erp.Models.Identity;
using erp.Models.TimeTracking;
using Microsoft.EntityFrameworkCore;

namespace erp.Services.TimeTracking;

public interface ITimeTrackingService
{
    Task<List<PayrollPeriod>> GetPeriodsAsync(int? year, CancellationToken cancellationToken = default);
    Task<PayrollPeriod?> GetPeriodAsync(int id, CancellationToken cancellationToken = default);
    Task<PayrollPeriod?> GetPeriodByReferenceAsync(int month, int year, CancellationToken cancellationToken = default);
    Task<PayrollPeriod> CreatePeriodAsync(int month, int year, int createdById, CancellationToken cancellationToken = default);
    Task<PayrollEntry> UpdateEntryAsync(
        int entryId,
        decimal? faltas,
        decimal? abonos,
        decimal? horasExtras,
        decimal? atrasos,
        string? observacoes,
        int updatedById,
        CancellationToken cancellationToken = default);
    Task<PayrollEntry> AddEntryAsync(int periodId, int employeeId, CancellationToken cancellationToken = default);
    Task UpdateEntriesAsync(IEnumerable<BulkUpdatePayrollEntryDto> entries, int updatedById, CancellationToken cancellationToken = default);
}

public class TimeTrackingService : ITimeTrackingService
{
    private readonly ApplicationDbContext _context;

    public TimeTrackingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PayrollEntry> AddEntryAsync(int periodId, int employeeId, CancellationToken cancellationToken = default)
    {
        var existing = await _context.PayrollEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.PayrollPeriodId == periodId && e.EmployeeId == employeeId, cancellationToken);

        if (existing != null)
        {
            throw new InvalidOperationException("Colaborador já está no período.");
        }

        var entry = new PayrollEntry
        {
            PayrollPeriodId = periodId,
            EmployeeId = employeeId,
            CreatedAt = DateTime.UtcNow
        };

        _context.PayrollEntries.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);

        return await _context.PayrollEntries
            .Include(e => e.Employee)
            .FirstAsync(e => e.Id == entry.Id, cancellationToken);
    }

    public async Task<List<PayrollPeriod>> GetPeriodsAsync(int? year, CancellationToken cancellationToken = default)
    {
        var query = _context.PayrollPeriods
            .AsNoTracking()
            .Include(p => p.Entries)
            .OrderByDescending(p => p.ReferenceYear)
            .ThenByDescending(p => p.ReferenceMonth)
            .AsQueryable();

        if (year.HasValue)
        {
            query = query.Where(p => p.ReferenceYear == year.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public Task<PayrollPeriod?> GetPeriodAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.PayrollPeriods
            .AsNoTracking()
            .Include(p => p.Entries)
                .ThenInclude(e => e.Employee)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public Task<PayrollPeriod?> GetPeriodByReferenceAsync(int month, int year, CancellationToken cancellationToken = default)
    {
        return _context.PayrollPeriods
            .AsNoTracking()
            .Include(p => p.Entries)
                .ThenInclude(e => e.Employee)
            .FirstOrDefaultAsync(p => p.ReferenceMonth == month && p.ReferenceYear == year, cancellationToken);
    }

    public async Task<PayrollPeriod> CreatePeriodAsync(int month, int year, int createdById, CancellationToken cancellationToken = default)
    {
        var existing = await _context.PayrollPeriods
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ReferenceMonth == month && p.ReferenceYear == year, cancellationToken);

        if (existing != null)
        {
            throw new InvalidOperationException("Já existe um apontamento para o período informado.");
        }

        var employees = await _context.Set<ApplicationUser>()
            .AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.FullName ?? u.UserName)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;

        var period = new PayrollPeriod
        {
            ReferenceMonth = month,
            ReferenceYear = year,
            Status = PayrollPeriodStatus.Draft,
            CreatedAt = now,
            CreatedById = createdById,
            UpdatedAt = now,
            UpdatedById = createdById,
            Entries = employees.Select(id => new PayrollEntry
            {
                EmployeeId = id,
                CreatedAt = now
            }).ToList()
        };

        _context.PayrollPeriods.Add(period);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetPeriodAsync(period.Id, cancellationToken)
            ?? throw new InvalidOperationException("Erro ao carregar o apontamento recém-criado.");
    }

    public async Task<PayrollEntry> UpdateEntryAsync(
        int entryId,
        decimal? faltas,
        decimal? abonos,
        decimal? horasExtras,
        decimal? atrasos,
        string? observacoes,
        int updatedById,
        CancellationToken cancellationToken = default)
    {
        var entry = await _context.PayrollEntries
            .Include(e => e.Employee)
            .Include(e => e.PayrollPeriod)
            .FirstOrDefaultAsync(e => e.Id == entryId, cancellationToken);

        if (entry == null)
        {
            throw new KeyNotFoundException("Apontamento não encontrado.");
        }

        entry.Faltas = NormalizeDecimal(faltas);
        entry.Abonos = NormalizeDecimal(abonos);
        entry.HorasExtras = NormalizeDecimal(horasExtras);
        entry.Atrasos = NormalizeDecimal(atrasos);
        entry.Observacoes = string.IsNullOrWhiteSpace(observacoes) ? null : observacoes.Trim();
        entry.UpdatedAt = DateTime.UtcNow;
        entry.UpdatedById = updatedById;

        if (entry.PayrollPeriod != null)
        {
            entry.PayrollPeriod.UpdatedAt = entry.UpdatedAt;
            entry.PayrollPeriod.UpdatedById = updatedById;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return entry;
    }

    public async Task UpdateEntriesAsync(IEnumerable<BulkUpdatePayrollEntryDto> entries, int updatedById, CancellationToken cancellationToken = default)
    {
        var ids = entries.Select(e => e.Id).ToList();
        var dbEntries = await _context.PayrollEntries
            .Where(e => ids.Contains(e.Id))
            .Include(e => e.PayrollPeriod)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var entriesDict = entries.ToDictionary(e => e.Id);

        foreach (var dbEntry in dbEntries)
        {
            if (entriesDict.TryGetValue(dbEntry.Id, out var dto))
            {
                dbEntry.Faltas = NormalizeDecimal(dto.Faltas);
                dbEntry.Abonos = NormalizeDecimal(dto.Abonos);
                dbEntry.HorasExtras = NormalizeDecimal(dto.HorasExtras);
                dbEntry.Atrasos = NormalizeDecimal(dto.Atrasos);
                dbEntry.Observacoes = string.IsNullOrWhiteSpace(dto.Observacoes) ? null : dto.Observacoes.Trim();
                dbEntry.UpdatedAt = now;
                dbEntry.UpdatedById = updatedById;

                if (dbEntry.PayrollPeriod != null)
                {
                    dbEntry.PayrollPeriod.UpdatedAt = now;
                    dbEntry.PayrollPeriod.UpdatedById = updatedById;
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static decimal? NormalizeDecimal(decimal? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return decimal.Round(value.Value, 2, MidpointRounding.AwayFromZero);
    }
}
