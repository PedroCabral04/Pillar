using erp.DTOs.Reports;

namespace erp.Services.Reports;

public interface IFinancialReportService
{
    Task<CashFlowReportDto> GenerateCashFlowReportAsync(FinancialReportFilterDto filter);
    Task<ProfitLossReportDto> GenerateProfitLossReportAsync(FinancialReportFilterDto filter);
    Task<BalanceSheetReportDto> GenerateBalanceSheetReportAsync(FinancialReportFilterDto filter);
}
