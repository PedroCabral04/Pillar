using erp.DTOs.Financial;
using erp.Models.Financial;

namespace erp.Services.Financial;

public interface ICommissionService
{
    /// <summary>
    /// Calculates and creates commissions for all items in a finalized sale
    /// </summary>
    Task CalculateCommissionsForSaleAsync(int saleId);

    /// <summary>
    /// Gets commission summary for a user in a specific month/year
    /// </summary>
    Task<CommissionSummaryDto> GetUserCommissionsByMonthAsync(int userId, int year, int month);

    /// <summary>
    /// Gets all commissions with optional filters
    /// </summary>
    Task<List<CommissionDto>> GetCommissionsAsync(int? userId = null, CommissionStatus? status = null, int? year = null, int? month = null);

    /// <summary>
    /// Marks a commission as paid
    /// </summary>
    Task MarkCommissionAsPaidAsync(int commissionId, int payrollId);

    /// <summary>
    /// Cancels commissions for a cancelled sale
    /// </summary>
    Task CancelCommissionsForSaleAsync(int saleId);

    /// <summary>
    /// Calculates and creates commissions for a completed service order
    /// </summary>
    Task CalculateCommissionsForServiceOrderAsync(int serviceOrderId);

    /// <summary>
    /// Cancels commissions for a cancelled service order
    /// </summary>
    Task CancelCommissionsForServiceOrderAsync(int serviceOrderId);
}
