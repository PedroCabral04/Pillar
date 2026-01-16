using erp.Data;
using erp.Models.Identity;
using erp.Models.Payroll;
using erp.Models.TimeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace erp.Services.Payroll;

public interface IPayrollCalculationService
{
    Task<PayrollPeriod> CalculateAsync(int payrollPeriodId, int requestedById, CancellationToken cancellationToken = default);
}

public class PayrollCalculationService : IPayrollCalculationService
{
    private const decimal MonthlyWorkloadHours = 220m;
    private const decimal OvertimeMultiplier = 1.5m;
    private const decimal DependentDeduction = 189.59m;

    private readonly ApplicationDbContext _context;
    private readonly ILogger<PayrollCalculationService> _logger;

    public PayrollCalculationService(ApplicationDbContext context, ILogger<PayrollCalculationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PayrollPeriod> CalculateAsync(int payrollPeriodId, int requestedById, CancellationToken cancellationToken = default)
    {
        var period = await _context.PayrollPeriods
            .Include(p => p.Entries)
                .ThenInclude(e => e.Employee)
            .Include(p => p.Results)
                .ThenInclude(r => r.Components)
            .FirstOrDefaultAsync(p => p.Id == payrollPeriodId, cancellationToken);

        if (period == null)
        {
            throw new KeyNotFoundException("Período de folha não encontrado.");
        }

        if (period.Status is PayrollPeriodStatus.Approved or PayrollPeriodStatus.Paid or PayrollPeriodStatus.Locked)
        {
            throw new InvalidOperationException("Não é possível recalcular uma folha já aprovada ou paga.");
        }

        if (!period.Entries.Any())
        {
            throw new InvalidOperationException("Não há apontamentos para calcular neste período.");
        }

        if (period.Results.Any())
        {
            _context.PayrollResults.RemoveRange(period.Results);
            period.Results.Clear();
        }

        var calculationDate = DateTime.UtcNow;
        var inssBrackets = await GetBracketsAsync(PayrollTaxType.Inss, calculationDate, cancellationToken);
        var irrfBrackets = await GetBracketsAsync(PayrollTaxType.Irrf, calculationDate, cancellationToken);

        var orderedEntries = period.Entries
            .OrderBy(e => e.Employee?.FullName ?? e.Employee?.UserName ?? $"#{e.EmployeeId}")
            .ToList();

        var results = new List<PayrollResult>(orderedEntries.Count);

        foreach (var entry in orderedEntries)
        {
            if (entry.Employee == null)
            {
                _logger.LogWarning("Funcionário {EmployeeId} não encontrado para o período {PeriodId}", entry.EmployeeId, period.Id);
                continue;
            }

            var employee = entry.Employee;
            var baseSalary = Math.Max(employee.Salary ?? 0m, 0m);
            var hourlyRate = MonthlyWorkloadHours <= 0 ? 0 : decimal.Round(baseSalary / MonthlyWorkloadHours, 4, MidpointRounding.AwayFromZero);

            var overtimeHours = entry.HorasExtras ?? 0m;
            var abonoAmount = DecimalRound(entry.Abonos ?? 0m);
            var faltasHours = entry.Faltas ?? 0m;
            var atrasosHours = entry.Atrasos ?? 0m;

            var overtimeAmount = DecimalRound(hourlyRate * overtimeHours * OvertimeMultiplier);
            var faltasAmount = DecimalRound(hourlyRate * faltasHours);
            var atrasosAmount = DecimalRound(hourlyRate * atrasosHours);

            var earningsTotal = DecimalRound(baseSalary + overtimeAmount + abonoAmount);
            var preTaxDeductions = DecimalRound(faltasAmount + atrasosAmount);
            var grossAmount = DecimalRound(Math.Max(earningsTotal - preTaxDeductions, 0));

            var inssAmount = CalculateProgressiveTax(grossAmount, inssBrackets, isSimpleDeduction: false);
            var dependents = employee.DependentCount;
            var irrfBase = Math.Max(grossAmount - inssAmount - (dependents * DependentDeduction), 0);
            var irrfAmount = CalculateProgressiveTax(irrfBase, irrfBrackets, isSimpleDeduction: true);

            var totalDeductions = DecimalRound(preTaxDeductions + inssAmount + irrfAmount);
            var netAmount = DecimalRound(Math.Max(earningsTotal - totalDeductions, 0));

            var result = new PayrollResult
            {
                PayrollPeriodId = period.Id,
                EmployeeId = employee.Id,
                Employee = employee,
                PayrollEntryId = entry.Id,
                EmployeeNameSnapshot = employee.FullName ?? employee.UserName ?? $"Colaborador #{employee.Id}",
                EmployeeCpfSnapshot = employee.Cpf,
                DepartmentSnapshot = employee.Department?.Name,
                PositionSnapshot = employee.Position?.Title,
                BankNameSnapshot = employee.BankName,
                BankAgencySnapshot = employee.BankAgency,
                BankAccountSnapshot = employee.BankAccount,
                DependentsSnapshot = dependents,
                BaseSalarySnapshot = baseSalary,
                TotalEarnings = earningsTotal,
                TotalDeductions = totalDeductions,
                TotalContributions = inssAmount,
                GrossAmount = grossAmount,
                NetAmount = netAmount,
                InssAmount = inssAmount,
                IrrfAmount = irrfAmount,
                CreatedAt = calculationDate,
                UpdatedAt = calculationDate,
                UpdatedById = requestedById
            };

            var components = new List<PayrollComponent>();
            var sequence = 10;

            components.Add(CreateComponent(result, PayrollComponentType.Earning, "BASE", "Salário base", baseSalary, sequence += 10, baseSalary, null, impactsFgts: true, taxable: true));

            if (overtimeAmount > 0)
            {
                components.Add(CreateComponent(result, PayrollComponentType.Earning, "HE", "Horas extras", overtimeAmount, sequence += 10, baseSalary, overtimeHours, true, true));
            }

            if (abonoAmount > 0)
            {
                components.Add(CreateComponent(result, PayrollComponentType.Earning, "ABONO", "Bonificações", abonoAmount, sequence += 10, null, null, true, true));
            }

            if (faltasAmount > 0)
            {
                components.Add(CreateComponent(result, PayrollComponentType.Deduction, "FALTAS", "Faltas", faltasAmount, sequence += 10, null, faltasHours, false, false));
            }

            if (atrasosAmount > 0)
            {
                components.Add(CreateComponent(result, PayrollComponentType.Deduction, "ATRASO", "Atrasos", atrasosAmount, sequence += 10, null, atrasosHours, false, false));
            }

            if (inssAmount > 0)
            {
                components.Add(CreateComponent(result, PayrollComponentType.Deduction, "INSS", "INSS", inssAmount, sequence += 10, grossAmount, null, true, true));
            }

            if (irrfAmount > 0)
            {
                components.Add(CreateComponent(result, PayrollComponentType.Deduction, "IRRF", "IRRF", irrfAmount, sequence += 10, grossAmount, null, false, true));
            }

            result.Components = components;
            results.Add(result);
        }

        if (!results.Any())
        {
            throw new InvalidOperationException("Nenhum resultado foi calculado. Verifique se os colaboradores possuem salário configurado.");
        }

        period.Results = results;
        period.CalculationDate = calculationDate;
        period.UpdatedAt = calculationDate;
        period.UpdatedById = requestedById;
        period.TotalGrossAmount = results.Sum(r => r.GrossAmount);
        period.TotalNetAmount = results.Sum(r => r.NetAmount);
        period.TotalInssAmount = results.Sum(r => r.InssAmount);
        period.TotalIrrfAmount = results.Sum(r => r.IrrfAmount);
        period.TotalEmployerCost = results.Sum(r => r.GrossAmount + (r.AdditionalEmployerCost ?? 0));
        period.Status = PayrollPeriodStatus.Calculated;

        _context.PayrollResults.AddRange(results);
        await _context.SaveChangesAsync(cancellationToken);

        return period;
    }

    private static PayrollComponent CreateComponent(
        PayrollResult result,
        PayrollComponentType type,
        string code,
        string description,
        decimal amount,
        int sequence,
        decimal? baseAmount,
        decimal? referenceQuantity,
        bool impactsFgts,
        bool taxable)
    {
        return new PayrollComponent
        {
            PayrollResult = result,
            Type = type,
            Code = code,
            Description = description,
            Amount = DecimalRound(amount),
            BaseAmount = baseAmount.HasValue ? DecimalRound(baseAmount.Value) : null,
            ReferenceQuantity = referenceQuantity,
            ImpactsFgts = impactsFgts,
            IsTaxable = taxable,
            Sequence = sequence
        };
    }

    private async Task<List<PayrollTaxBracket>> GetBracketsAsync(PayrollTaxType taxType, DateTime referenceDate, CancellationToken cancellationToken)
    {
        var brackets = await _context.PayrollTaxBrackets
            .AsNoTracking()
            .Where(b => b.TaxType == taxType && b.IsActive && b.EffectiveFrom <= referenceDate && (b.EffectiveTo == null || b.EffectiveTo >= referenceDate))
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.RangeStart)
            .ToListAsync(cancellationToken);

        if (brackets.Count == 0)
        {
            brackets = GetDefaultBrackets(taxType);
        }

        return brackets;
    }

    private static List<PayrollTaxBracket> GetDefaultBrackets(PayrollTaxType taxType)
    {
        return taxType switch
        {
            PayrollTaxType.Inss => new List<PayrollTaxBracket>
            {
                new() { RangeStart = 0m, RangeEnd = 1412.00m, Rate = 0.075m, Deduction = 0m, SortOrder = 1 },
                new() { RangeStart = 1412.01m, RangeEnd = 2666.68m, Rate = 0.09m, Deduction = 0m, SortOrder = 2 },
                new() { RangeStart = 2666.69m, RangeEnd = 4000.03m, Rate = 0.12m, Deduction = 0m, SortOrder = 3 },
                new() { RangeStart = 4000.04m, RangeEnd = 7786.02m, Rate = 0.14m, Deduction = 0m, SortOrder = 4 },
            },
            PayrollTaxType.Irrf => new List<PayrollTaxBracket>
            {
                new() { RangeStart = 0m, RangeEnd = 2259.20m, Rate = 0m, Deduction = 0m, SortOrder = 1 },
                new() { RangeStart = 2259.21m, RangeEnd = 2826.65m, Rate = 0.075m, Deduction = 169.44m, SortOrder = 2 },
                new() { RangeStart = 2826.66m, RangeEnd = 3751.05m, Rate = 0.15m, Deduction = 381.44m, SortOrder = 3 },
                new() { RangeStart = 3751.06m, RangeEnd = 4664.68m, Rate = 0.225m, Deduction = 662.77m, SortOrder = 4 },
                new() { RangeStart = 4664.69m, RangeEnd = null, Rate = 0.275m, Deduction = 896m, SortOrder = 5 }
            },
            _ => new List<PayrollTaxBracket>()
        };
    }

    private static decimal CalculateProgressiveTax(decimal baseAmount, IReadOnlyList<PayrollTaxBracket> brackets, bool isSimpleDeduction = false)
    {
        if (baseAmount <= 0 || brackets.Count == 0)
        {
            return 0m;
        }

        if (isSimpleDeduction)
        {
            // Lógica IRRF: Base Total * Alíquota da Faixa - Parcela a Deduzir
            var appliedBracket = brackets
                .OrderByDescending(b => b.RangeStart)
                .FirstOrDefault(b => baseAmount >= b.RangeStart);

            if (appliedBracket == null) return 0m;

            var tax = (baseAmount * appliedBracket.Rate) - appliedBracket.Deduction;
            return DecimalRound(Math.Max(tax, 0));
        }
        else
        {
            // Lógica INSS: Soma progressiva das fatias
            decimal total = 0m;
            foreach (var bracket in brackets)
            {
                if (baseAmount <= bracket.RangeStart) break;

                var upperLimit = bracket.RangeEnd ?? decimal.MaxValue;
                var taxablePortion = Math.Min(baseAmount, upperLimit) - bracket.RangeStart;
                
                if (taxablePortion > 0)
                {
                    total += taxablePortion * bracket.Rate;
                }

                if (baseAmount <= upperLimit) break;
            }
            return DecimalRound(Math.Max(total, 0));
        }
    }

    private static decimal DecimalRound(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
