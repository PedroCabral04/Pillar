using System.Collections.Generic;
using System.Linq;
using erp.DTOs.TimeTracking;
using erp.Models.TimeTracking;
using Riok.Mapperly.Abstractions;

namespace erp.Mappings;

[Mapper]
public partial class TimeTrackingMapper
{
    [MapProperty(nameof(PayrollPeriod.Status), nameof(PayrollPeriodSummaryDto.Status))]
    [MapProperty(nameof(PayrollPeriod.Status), nameof(PayrollPeriodSummaryDto.StatusName))]
    [MapProperty(nameof(PayrollPeriod.Entries), nameof(PayrollPeriodSummaryDto.TotalEmployees))]
    public partial PayrollPeriodSummaryDto ToSummaryDto(PayrollPeriod period);

    [MapProperty(nameof(PayrollPeriod.Status), nameof(PayrollPeriodDetailDto.Status))]
    [MapProperty(nameof(PayrollPeriod.Status), nameof(PayrollPeriodDetailDto.StatusName))]
    [MapProperty(nameof(PayrollPeriod.Entries), nameof(PayrollPeriodDetailDto.TotalEmployees))]
    [MapProperty(nameof(PayrollPeriod.Entries), nameof(PayrollPeriodDetailDto.Entries))]
    public partial PayrollPeriodDetailDto ToDetailDto(PayrollPeriod period);

    [MapProperty(nameof(PayrollEntry.Employee.FullName), nameof(PayrollEntryDto.EmployeeName))]
    [MapProperty(nameof(PayrollEntry.Employee.Email), nameof(PayrollEntryDto.EmployeeEmail))]
    public partial PayrollEntryDto ToEntryDto(PayrollEntry entry);

    public partial IEnumerable<PayrollPeriodSummaryDto> ToSummaryDto(IEnumerable<PayrollPeriod> periods);
    public partial IEnumerable<PayrollEntryDto> ToEntryDto(IEnumerable<PayrollEntry> entries);

    private string MapStatusToStatusName(PayrollPeriodStatus status) => status switch
    {
        PayrollPeriodStatus.Draft => "Em preenchimento",
        PayrollPeriodStatus.Calculated => "Calculado",
        PayrollPeriodStatus.Approved => "Aprovado",
        PayrollPeriodStatus.Paid => "Pago",
        PayrollPeriodStatus.Locked => "Fechado",
        _ => status.ToString()
    };

    private int MapEntriesToTotalEmployees(ICollection<PayrollEntry> entries) => entries?.Count ?? 0;

    private List<PayrollEntryDto> MapEntriesToEntries(ICollection<PayrollEntry> entries) =>
        entries?.Select(ToEntryDto).ToList() ?? new List<PayrollEntryDto>();
}
