using erp.DTOs.Reports;

namespace erp.Services.Reports;

public interface ISalesReportService
{
    Task<SalesReportResultDto> GenerateSalesReportAsync(SalesReportFilterDto filter);
    Task<CustomerSalesReportResultDto> GenerateByCustomerReportAsync(SalesReportFilterDto filter);
    Task<ProductSalesReportResultDto> GenerateByProductReportAsync(SalesReportFilterDto filter);
    Task<PaymentMethodSalesReportResultDto> GenerateByPaymentMethodReportAsync(SalesReportFilterDto filter);
}
