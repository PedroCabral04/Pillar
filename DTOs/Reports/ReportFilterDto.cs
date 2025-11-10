namespace erp.DTOs.Reports;

/// <summary>
/// Base filter DTO for all reports
/// </summary>
public class ReportFilterDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? ExportFormat { get; set; } // "pdf" or "excel"
}
