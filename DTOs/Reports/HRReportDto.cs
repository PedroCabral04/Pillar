namespace erp.DTOs.Reports;

/// <summary>
/// DTO for HR report filters
/// </summary>
public class HRReportFilterDto : ReportFilterDto
{
    public string ReportType { get; set; } = "attendance"; // attendance, turnover, headcount
    public int? DepartmentId { get; set; }
    public int? PositionId { get; set; }
    public int? EmployeeId { get; set; }
    public string? ContractType { get; set; }
}

/// <summary>
/// DTO for attendance report
/// </summary>
public class AttendanceReportDto
{
    public List<AttendanceReportItemDto> Items { get; set; } = new();
    public AttendanceReportSummaryDto Summary { get; set; } = new();
}

public class AttendanceReportItemDto
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public int WorkDays { get; set; }
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int LateDays { get; set; }
    public decimal TotalHoursWorked { get; set; }
    public decimal AttendanceRate { get; set; }
}

public class AttendanceReportSummaryDto
{
    public int TotalEmployees { get; set; }
    public decimal AverageAttendanceRate { get; set; }
    public int TotalAbsences { get; set; }
    public int TotalLateArrivals { get; set; }
}

/// <summary>
/// DTO for turnover report
/// </summary>
public class TurnoverReportDto
{
    public List<TurnoverReportItemDto> Items { get; set; } = new();
    public TurnoverReportSummaryDto Summary { get; set; } = new();
}

public class TurnoverReportItemDto
{
    public string Period { get; set; } = string.Empty;
    public int EmployeesAtStart { get; set; }
    public int NewHires { get; set; }
    public int Terminations { get; set; }
    public int EmployeesAtEnd { get; set; }
    public decimal TurnoverRate { get; set; }
}

public class TurnoverReportSummaryDto
{
    public int CurrentHeadcount { get; set; }
    public int TotalHires { get; set; }
    public int TotalTerminations { get; set; }
    public decimal AverageTurnoverRate { get; set; }
    public Dictionary<string, int> TerminationsByReason { get; set; } = new();
}

/// <summary>
/// DTO for headcount report
/// </summary>
public class HeadcountReportDto
{
    public List<HeadcountByDepartmentDto> ByDepartment { get; set; } = new();
    public List<HeadcountByPositionDto> ByPosition { get; set; } = new();
    public List<HeadcountByContractTypeDto> ByContractType { get; set; } = new();
    public HeadcountSummaryDto Summary { get; set; } = new();
}

public class HeadcountByDepartmentDto
{
    public string Department { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public decimal Percentage { get; set; }
}

public class HeadcountByPositionDto
{
    public string Position { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public decimal Percentage { get; set; }
}

public class HeadcountByContractTypeDto
{
    public string ContractType { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public decimal Percentage { get; set; }
}

public class HeadcountSummaryDto
{
    public int TotalEmployees { get; set; }
    public int TotalDepartments { get; set; }
    public int TotalPositions { get; set; }
    public decimal AverageAge { get; set; }
    public decimal AverageTenure { get; set; }
}
