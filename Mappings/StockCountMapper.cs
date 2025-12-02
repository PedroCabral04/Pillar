using Riok.Mapperly.Abstractions;
using erp.Models.Inventory;
using erp.DTOs.Inventory;
using System.Diagnostics.CodeAnalysis;

namespace erp.Mappings;

[Mapper]
[SuppressMessage("Mapper", "RMG020")]
public partial class StockCountMapper
{
    // StockCount -> StockCountDto
    [UserMapping(Default = true)]
    public partial StockCountDto CountToCountDto(StockCount count);
    public partial IEnumerable<StockCountDto> CountsToCountDtos(IEnumerable<StockCount> counts);
    
    // StockCountItem -> StockCountItemDto
    [UserMapping(Default = true)]
    public partial StockCountItemDto ItemToItemDto(StockCountItem item);
    public partial IEnumerable<StockCountItemDto> ItemsToItemDtos(IEnumerable<StockCountItem> items);
    
    // CreateStockCountDto -> StockCount
    [MapperIgnoreTarget(nameof(StockCount.Id))]
    [MapperIgnoreTarget(nameof(StockCount.Status))]
    [MapperIgnoreTarget(nameof(StockCount.ClosedDate))]
    [MapperIgnoreTarget(nameof(StockCount.CreatedByUserId))]
    [MapperIgnoreTarget(nameof(StockCount.ApprovedByUserId))]
    [MapperIgnoreTarget(nameof(StockCount.Warehouse))]
    [MapperIgnoreTarget(nameof(StockCount.CreatedByUser))]
    [MapperIgnoreTarget(nameof(StockCount.ApprovedByUser))]
    [MapperIgnoreTarget(nameof(StockCount.Items))]
    public partial StockCount CreateCountDtoToCount(CreateStockCountDto dto);
    
    // AddStockCountItemDto -> StockCountItem
    [MapperIgnoreTarget(nameof(StockCountItem.Id))]
    [MapperIgnoreTarget(nameof(StockCountItem.SystemStock))]
    [MapperIgnoreTarget(nameof(StockCountItem.StockCount))]
    [MapperIgnoreTarget(nameof(StockCountItem.Product))]
    public partial StockCountItem AddItemDtoToItem(AddStockCountItemDto dto);
    
    // Mapeamento customizado para incluir nomes e cálculos
    public StockCountDto MapWithDetails(StockCount count)
    {
        var dto = CountToCountDto(count);
        dto.WarehouseName = count.Warehouse?.Name;
        dto.StatusName = count.Status.ToString();
        dto.CreatedByUserName = count.CreatedByUser?.UserName ?? "";
        dto.ApprovedByUserName = count.ApprovedByUser?.UserName;
        
        // Mapear itens com detalhes
        dto.Items = count.Items.Select(MapItemWithDetails).ToList();
        
        return dto;
    }
    
    public StockCountItemDto MapItemWithDetails(StockCountItem item)
    {
        var dto = ItemToItemDto(item);
        dto.ProductName = item.Product?.Name ?? "";
        dto.ProductSku = item.Product?.Sku ?? "";
        dto.Difference = item.PhysicalStock - item.SystemStock;
        dto.DifferencePercentage = item.SystemStock > 0 
            ? (dto.Difference / item.SystemStock) * 100 
            : 0;
        dto.UnitCost = item.Product?.CostPrice ?? 0;
        dto.DifferenceValue = dto.Difference * dto.UnitCost;
        
        return dto;
    }
}
