using erp.DTOs.Sales;

namespace erp.Services.Sales;

public interface ICustomerService
{
    Task<CustomerDto> CreateAsync(CreateCustomerDto dto);
    Task<CustomerDto?> GetByIdAsync(int id);
    Task<(List<CustomerDto> items, int total)> SearchAsync(string? search, bool? isActive, int page, int pageSize);
    Task<CustomerDto> UpdateAsync(int id, UpdateCustomerDto dto);
    Task<bool> DeleteAsync(int id);
    Task<CustomerDto?> GetByDocumentAsync(string document);
}
