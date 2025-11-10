using erp.Data;
using erp.DTOs.Reports;
using Microsoft.EntityFrameworkCore;

namespace erp.Services.Reports;

public class HRReportService : IHRReportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HRReportService> _logger;

    public HRReportService(ApplicationDbContext context, ILogger<HRReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AttendanceReportDto> GenerateAttendanceReportAsync(HRReportFilterDto filter)
    {
        try
        {
            // Get payroll entries for attendance calculation
            var query = _context.PayrollEntries
                .Include(t => t.Employee)
                    .ThenInclude(u => u!.Department)
                .Include(t => t.Employee)
                    .ThenInclude(u => u!.Position)
                .Include(t => t.PayrollPeriod)
                .AsQueryable();

            // Filter by date range using ReferenceMonth and ReferenceYear
            if (filter.StartDate.HasValue || filter.EndDate.HasValue)
            {
                var startYear = filter.StartDate?.Year ?? DateTime.MinValue.Year;
                var startMonth = filter.StartDate?.Month ?? 1;
                var endYear = filter.EndDate?.Year ?? DateTime.MaxValue.Year;
                var endMonth = filter.EndDate?.Month ?? 12;

                query = query.Where(t => 
                    (t.PayrollPeriod.ReferenceYear > startYear || 
                     (t.PayrollPeriod.ReferenceYear == startYear && t.PayrollPeriod.ReferenceMonth >= startMonth)) &&
                    (t.PayrollPeriod.ReferenceYear < endYear || 
                     (t.PayrollPeriod.ReferenceYear == endYear && t.PayrollPeriod.ReferenceMonth <= endMonth)));
            }

            if (filter.EmployeeId.HasValue)
            {
                query = query.Where(t => t.EmployeeId == filter.EmployeeId.Value);
            }

            if (filter.DepartmentId.HasValue)
            {
                query = query.Where(t => t.Employee.DepartmentId == filter.DepartmentId.Value);
            }

            var entries = await query.ToListAsync();

            // Group by employee
            var items = entries
                .GroupBy(e => new
                {
                    e.EmployeeId,
                    EmployeeName = e.Employee.FullName ?? e.Employee.UserName ?? "N/A",
                    Department = e.Employee.Department?.Name ?? "Sem departamento",
                    Position = e.Employee.Position?.Title ?? "Sem cargo"
                })
                .Select(g =>
                {
                    var totalAbsences = g.Sum(e => e.Faltas ?? 0);
                    var totalLateArrivals = g.Sum(e => e.Atrasos ?? 0);
                    var workPeriods = g.Count();
                    
                    return new AttendanceReportItemDto
                    {
                        EmployeeId = g.Key.EmployeeId,
                        EmployeeName = g.Key.EmployeeName,
                        Department = g.Key.Department,
                        Position = g.Key.Position,
                        WorkDays = workPeriods * 22, // Assuming ~22 work days per period
                        PresentDays = (int)(workPeriods * 22 - totalAbsences),
                        AbsentDays = (int)totalAbsences,
                        LateDays = (int)totalLateArrivals,
                        TotalHoursWorked = g.Sum(e => e.HorasExtras ?? 0) + (workPeriods * 176m), // 176 = 22 days * 8 hours
                        AttendanceRate = totalAbsences > 0 ? ((workPeriods * 22m - totalAbsences) / (workPeriods * 22m) * 100) : 100
                    };
                })
                .ToList();

            var summary = new AttendanceReportSummaryDto
            {
                TotalEmployees = items.Count,
                AverageAttendanceRate = items.Any() ? items.Average(i => i.AttendanceRate) : 0,
                TotalAbsences = items.Sum(i => i.AbsentDays),
                TotalLateArrivals = items.Sum(i => i.LateDays)
            };

            return new AttendanceReportDto
            {
                Items = items,
                Summary = summary
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório de presença");
            throw;
        }
    }

    public async Task<TurnoverReportDto> GenerateTurnoverReportAsync(HRReportFilterDto filter)
    {
        try
        {
            // Note: This is a simplified version. A real implementation would need
            // termination tracking and hire date tracking
            
            var items = new List<TurnoverReportItemDto>();
            var startDate = filter.StartDate ?? DateTime.UtcNow.AddYears(-1);
            var endDate = filter.EndDate ?? DateTime.UtcNow;

            // Get current headcount
            var currentHeadcount = await _context.Set<erp.Models.Identity.ApplicationUser>()
                .Where(u => u.Email != null) // Simple check for active users
                .CountAsync();

            // Create a simple month-by-month report
            var currentDate = new DateTime(startDate.Year, startDate.Month, 1);
            while (currentDate <= endDate)
            {
                var monthEnd = currentDate.AddMonths(1).AddDays(-1);
                
                var hiredInMonth = await _context.Set<erp.Models.Identity.ApplicationUser>()
                    .Where(u => u.CreatedAt >= currentDate && u.CreatedAt <= monthEnd)
                    .CountAsync();

                items.Add(new TurnoverReportItemDto
                {
                    Period = currentDate.ToString("MM/yyyy"),
                    EmployeesAtStart = currentHeadcount,
                    NewHires = hiredInMonth,
                    Terminations = 0, // Would need termination date tracking
                    EmployeesAtEnd = currentHeadcount + hiredInMonth,
                    TurnoverRate = 0 // Calculated as (terminations / avg headcount) * 100
                });

                currentDate = currentDate.AddMonths(1);
            }

            var summary = new TurnoverReportSummaryDto
            {
                CurrentHeadcount = currentHeadcount,
                TotalHires = items.Sum(i => i.NewHires),
                TotalTerminations = items.Sum(i => i.Terminations),
                AverageTurnoverRate = items.Any() ? items.Average(i => i.TurnoverRate) : 0,
                TerminationsByReason = new Dictionary<string, int>() // Would need termination reason tracking
            };

            return new TurnoverReportDto
            {
                Items = items,
                Summary = summary
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório de turnover");
            throw;
        }
    }

    public async Task<HeadcountReportDto> GenerateHeadcountReportAsync(HRReportFilterDto filter)
    {
        try
        {
            // Query ApplicationUser (Identity) instead of legacy User
            var query = _context.Set<erp.Models.Identity.ApplicationUser>()
                .Include(u => u.Department)
                .Include(u => u.Position)
                .Where(u => u.Email != null) // Only active users
                .AsQueryable();

            if (filter.DepartmentId.HasValue)
            {
                query = query.Where(u => u.DepartmentId == filter.DepartmentId.Value);
            }

            if (filter.PositionId.HasValue)
            {
                query = query.Where(u => u.PositionId == filter.PositionId.Value);
            }

            if (!string.IsNullOrEmpty(filter.ContractType))
            {
                query = query.Where(u => u.ContractType == filter.ContractType);
            }

            var employees = await query.ToListAsync();

            var byDepartment = employees
                .GroupBy(e => e.Department?.Name ?? "Sem departamento")
                .Select(g => new HeadcountByDepartmentDto
                {
                    Department = g.Key,
                    EmployeeCount = g.Count(),
                    Percentage = employees.Any() ? (decimal)g.Count() / employees.Count * 100 : 0
                })
                .OrderByDescending(d => d.EmployeeCount)
                .ToList();

            var byPosition = employees
                .GroupBy(e => e.Position?.Title ?? "Sem cargo")
                .Select(g => new HeadcountByPositionDto
                {
                    Position = g.Key,
                    EmployeeCount = g.Count(),
                    Percentage = employees.Any() ? (decimal)g.Count() / employees.Count * 100 : 0
                })
                .OrderByDescending(p => p.EmployeeCount)
                .ToList();

            var byContractType = employees
                .GroupBy(e => e.ContractType ?? "Não especificado")
                .Select(g => new HeadcountByContractTypeDto
                {
                    ContractType = g.Key,
                    EmployeeCount = g.Count(),
                    Percentage = employees.Any() ? (decimal)g.Count() / employees.Count * 100 : 0
                })
                .OrderByDescending(c => c.EmployeeCount)
                .ToList();

            var today = DateTime.UtcNow;
            var averageTenureDays = employees
                .Select(e => (today - e.CreatedAt).TotalDays / 365.25)
                .Average();

            var summary = new HeadcountSummaryDto
            {
                TotalEmployees = employees.Count,
                TotalDepartments = byDepartment.Count,
                TotalPositions = byPosition.Count,
                AverageAge = 0, // Would need birthdate tracking
                AverageTenure = (decimal)averageTenureDays
            };

            return new HeadcountReportDto
            {
                ByDepartment = byDepartment,
                ByPosition = byPosition,
                ByContractType = byContractType,
                Summary = summary
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório de headcount");
            throw;
        }
    }
}
