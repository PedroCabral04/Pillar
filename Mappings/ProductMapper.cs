using Riok.Mapperly.Abstractions;
using erp.Models.Inventory;
using erp.DTOs.Inventory;
using System.Diagnostics.CodeAnalysis;

namespace erp.Mappings;

[Mapper]
[SuppressMessage("Mapper", "RMG020")]
public partial class ProductMapper
{
    // Product -> ProductDto
    public partial ProductDto ProductToProductDto(Product product);
    public partial IEnumerable<ProductDto> ProductsToProductDtos(IEnumerable<Product> products);
    
    // CreateProductDto -> Product (ignora campos calculados/automáticos)
    [MapperIgnoreTarget(nameof(Product.Id))]
    [MapperIgnoreTarget(nameof(Product.CurrentStock))]
    [MapperIgnoreTarget(nameof(Product.CreatedAt))]
    [MapperIgnoreTarget(nameof(Product.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Product.Category))]
    [MapperIgnoreTarget(nameof(Product.Brand))]
    [MapperIgnoreTarget(nameof(Product.Suppliers))]
    [MapperIgnoreTarget(nameof(Product.Images))]
    [MapperIgnoreTarget(nameof(Product.StockMovements))]
    [MapperIgnoreTarget(nameof(Product.CreatedByUser))]
    public partial Product CreateProductDtoToProduct(CreateProductDto dto);
    
    // UpdateProductDto -> Product (atualiza entidade existente)
    [MapperIgnoreTarget(nameof(Product.CurrentStock))]
    [MapperIgnoreTarget(nameof(Product.CreatedAt))]
    [MapperIgnoreTarget(nameof(Product.CreatedByUserId))]
    [MapperIgnoreTarget(nameof(Product.Category))]
    [MapperIgnoreTarget(nameof(Product.Brand))]
    [MapperIgnoreTarget(nameof(Product.Suppliers))]
    [MapperIgnoreTarget(nameof(Product.Images))]
    [MapperIgnoreTarget(nameof(Product.StockMovements))]
    [MapperIgnoreTarget(nameof(Product.CreatedByUser))]
    public partial void UpdateProductDtoToProduct(UpdateProductDto dto, Product product);
    
    // ProductCategory mappings
    public partial ProductCategoryDto CategoryToCategoryDto(ProductCategory category);
    public partial IEnumerable<ProductCategoryDto> CategoriesToCategoryDtos(IEnumerable<ProductCategory> categories);
    
    // ProductImage mappings
    public partial ProductImageDto ImageToImageDto(ProductImage image);
    public partial IEnumerable<ProductImageDto> ImagesToImageDtos(IEnumerable<ProductImage> images);
    
    // ProductSupplier mappings
    public partial ProductSupplierDto SupplierToSupplierDto(ProductSupplier supplier);
    public partial IEnumerable<ProductSupplierDto> SuppliersToSupplierDtos(IEnumerable<ProductSupplier> suppliers);
    
    // Mapeamento customizado para calcular ProfitMargin
    public ProductDto MapWithCalculations(Product product)
    {
        var dto = ProductToProductDto(product);
        dto.ProfitMargin = product.SalePrice > 0 
            ? (product.SalePrice - product.CostPrice) / product.SalePrice * 100 
            : 0;
        dto.StatusName = product.Status.ToString();
        dto.CategoryName = product.Category?.Name ?? "";
        dto.BrandName = product.Brand?.Name;
        dto.CreatedByUserName = product.CreatedByUser?.UserName ?? "";
        return dto;
    }
}
