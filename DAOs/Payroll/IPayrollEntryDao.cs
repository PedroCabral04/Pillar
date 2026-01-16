using erp.Models.TimeTracking;

namespace erp.DAOs.Payroll;

public interface IPayrollEntryDao
{
    Task<PayrollEntry?> GetByIdAsync(int id);
    Task<List<PayrollEntry>> GetByPeriodAsync(int periodId);
    Task<PayrollEntry> CreateAsync(PayrollEntry entry);
    Task<PayrollEntry> UpdateAsync(PayrollEntry entry);
    Task<bool> DeleteAsync(int id);
    Task<List<PayrollEntry>> GetByEmployeeAsync(int employeeId, int year, int month);
}
