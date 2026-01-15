using erp.Models.TimeTracking;

namespace erp.DAOs.Payroll;

public interface IPayrollPeriodDao
{
    Task<PayrollPeriod?> GetByIdAsync(int id);
    Task<PayrollPeriod?> GetByPeriodAsync(int year, int month);
    Task<List<PayrollPeriod>> GetAllAsync();
    Task<PayrollPeriod> CreateAsync(PayrollPeriod period);
    Task<PayrollPeriod> UpdateAsync(PayrollPeriod period);
    Task<bool> DeleteAsync(int id);
}
