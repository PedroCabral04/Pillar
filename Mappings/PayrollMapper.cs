using System.Collections.Generic;
using System.Linq;
using erp.DTOs.Payroll;
using erp.Models.Payroll;
using erp.Models.TimeTracking;
using Riok.Mapperly.Abstractions;

namespace erp.Mappings;

[Mapper]
public partial class PayrollMapper
{
    [MapProperty(nameof(PayrollPeriod.Status), nameof(PayrollPeriodListDto.Status))]
    [MapProperty(nameof(PayrollPeriod.Status), nameof(PayrollPeriodListDto.StatusName))]
    public partial PayrollPeriodListDto ToListDto(PayrollPeriod period);

    [MapProperty(nameof(PayrollPeriod.Results), nameof(PayrollPeriodDetailDto.Results))]
    [MapProperty(nameof(PayrollPeriod.Status), nameof(PayrollPeriodDetailDto.Status))]
    [MapProperty(nameof(PayrollPeriod.Status), nameof(PayrollPeriodDetailDto.StatusName))]
    public partial PayrollPeriodDetailDto ToDetailDto(PayrollPeriod period);

    [MapProperty(nameof(PayrollResult.EmployeeNameSnapshot), nameof(PayrollResultDto.EmployeeName))]
    [MapProperty(nameof(PayrollResult.EmployeeCpfSnapshot), nameof(PayrollResultDto.EmployeeCpf))]
    [MapProperty(nameof(PayrollResult.DepartmentSnapshot), nameof(PayrollResultDto.Department))]
    [MapProperty(nameof(PayrollResult.PositionSnapshot), nameof(PayrollResultDto.Position))]
    [MapProperty(nameof(PayrollResult.BaseSalarySnapshot), nameof(PayrollResultDto.BaseSalary))]
    [MapProperty(nameof(PayrollResult.Components), nameof(PayrollResultDto.Components))]
    public partial PayrollResultDto ToResultDto(PayrollResult result);

    public partial PayrollComponentDto ToComponentDto(PayrollComponent component);

    public partial IEnumerable<PayrollPeriodListDto> ToListDto(IEnumerable<PayrollPeriod> periods);
    public partial IEnumerable<PayrollResultDto> ToResultDto(IEnumerable<PayrollResult> results);

    private string MapStatusToStatusName(PayrollPeriodStatus status) => status switch
    {
        PayrollPeriodStatus.Draft => "Em preenchimento",
        PayrollPeriodStatus.Calculated => "Calculado",
        PayrollPeriodStatus.Approved => "Aprovado",
        PayrollPeriodStatus.Paid => "Pago",
        PayrollPeriodStatus.Locked => "Fechado",
        _ => status.ToString()
    };

}
