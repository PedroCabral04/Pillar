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
    [UserMapping(Default = true)]
    public partial ProductDto ProductToProductDto(Product product);
    public partial IEnumerable<ProductDto> ProductsToProductDtos(IEnumerable<Product> products);
    
    // CreateProductDto -> Product (ignora campos calculados/automáticos)
    [MapperIgnoreTarget(nameof(Product.Id))]
    [MapperIgnoreTarget(nameof(Product.CreatedAt))]
    [MapperIgnoreTarget(nameof(Product.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Product.Category))]
    [MapperIgnoreTarget(nameof(Product.Brand))]
    [MapperIgnoreTarget(nameof(Product.Suppliers))]
    [MapperIgnoreTarget(nameof(Product.Images))]
    [MapperIgnoreTarget(nameof(Product.StockMovements))]
    [MapperIgnoreTarget(nameof(Product.CreatedByUser))]
    [MapperIgnoreTarget(nameof(Product.VariantOptions))]
    [MapperIgnoreTarget(nameof(Product.Variants))]
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
    [MapperIgnoreTarget(nameof(Product.VariantOptions))]
    [MapperIgnoreTarget(nameof(Product.Variants))]
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
    
    // ProductVariant mappings
    public partial ProductVariantOptionDto OptionToOptionDto(ProductVariantOption option);
    public partial ProductVariantOptionValueDto OptionValueToOptionValueDto(ProductVariantOptionValue value);
    public partial ProductVariantDto VariantToVariantDto(ProductVariant variant);
    
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
        
        // Map variants with profit margin calculations
        if (product.HasVariants && product.Variants?.Any() == true)
        {
            dto.Variants = product.Variants.Select(v =>
            {
                var variantDto = VariantToVariantDto(v);
                variantDto.ProfitMargin = v.SalePrice > 0
                    ? (v.SalePrice - v.CostPrice) / v.SalePrice * 100
                    : 0;
                
                // Map option values from combinations
                if (v.Combinations?.Any() == true)
                {
                    variantDto.OptionValues = v.Combinations
                        .Where(c => c.OptionValue != null)
                        .Select(c => OptionValueToOptionValueDto(c.OptionValue))
                        .ToList();
                }
                
                return variantDto;
            }).ToList();
        }
        
        // Map variant options
        if (product.VariantOptions?.Any() == true)
        {
            dto.VariantOptions = product.VariantOptions
                .OrderBy(o => o.Position)
                .Select(o =>
                {
                    var optionDto = OptionToOptionDto(o);
                    optionDto.Values = o.Values
                        .OrderBy(v => v.Position)
                        .Select(OptionValueToOptionValueDto)
                        .ToList();
                    return optionDto;
                }).ToList();
        }
        
        return dto;
    }
}

