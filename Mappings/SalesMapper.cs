using Riok.Mapperly.Abstractions;
using erp.Models.Sales;
using erp.DTOs.Sales;

namespace erp.Mappings;

[Mapper]
public partial class SalesMapper
{
    // Customer mappings
    public partial CustomerDto ToDto(Customer customer);
    public partial Customer ToEntity(CreateCustomerDto dto);
    public partial void UpdateEntity(UpdateCustomerDto dto, Customer entity);
    
    // Sale mappings
    public partial SaleDto ToDto(Sale sale);
    
    [MapProperty(nameof(Sale.Customer.Name), nameof(SaleDto.CustomerName))]
    [MapProperty(nameof(Sale.User.UserName), nameof(SaleDto.UserName))]
    public partial SaleDto ToDtoWithRelations(Sale sale);
    
    // SaleItem mappings
    [UserMapping(Default = true)]
    public partial SaleItemDto ToDto(SaleItem item);
    
    [MapProperty(nameof(SaleItem.Product.Name), nameof(SaleItemDto.ProductName))]
    [MapProperty(nameof(SaleItem.Product.Sku), nameof(SaleItemDto.ProductSku))]
    public partial SaleItemDto ToDtoWithProduct(SaleItem item);
}
