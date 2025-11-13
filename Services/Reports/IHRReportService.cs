using erp.DTOs.Reports;

namespace erp.Services.Reports;

public interface IHRReportService
{
    Task<AttendanceReportDto> GenerateAttendanceReportAsync(HRReportFilterDto filter);
    Task<TurnoverReportDto> GenerateTurnoverReportAsync(HRReportFilterDto filter);
    Task<HeadcountReportDto> GenerateHeadcountReportAsync(HRReportFilterDto filter);
}
