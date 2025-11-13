using erp.DTOs.Reports;

namespace erp.Services.Reports;

public interface IInventoryReportService
{
    Task<StockLevelsReportDto> GenerateStockLevelsReportAsync(InventoryReportFilterDto filter);
    Task<StockMovementReportDto> GenerateStockMovementReportAsync(InventoryReportFilterDto filter);
    Task<InventoryValuationReportDto> GenerateInventoryValuationReportAsync(InventoryReportFilterDto filter);
}
