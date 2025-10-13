using Riok.Mapperly.Abstractions;
using erp.Models.Inventory;
using erp.DTOs.Inventory;
using System.Diagnostics.CodeAnalysis;

namespace erp.Mappings;

[Mapper]
[SuppressMessage("Mapper", "RMG020")]
public partial class StockMovementMapper
{
    // StockMovement -> StockMovementDto
    public partial StockMovementDto MovementToMovementDto(StockMovement movement);
    public partial IEnumerable<StockMovementDto> MovementsToMovementDtos(IEnumerable<StockMovement> movements);
    
    // CreateStockMovementDto -> StockMovement
    [MapperIgnoreTarget(nameof(StockMovement.Id))]
    [MapperIgnoreTarget(nameof(StockMovement.Product))]
    [MapperIgnoreTarget(nameof(StockMovement.Warehouse))]
    [MapperIgnoreTarget(nameof(StockMovement.PreviousStock))]
    [MapperIgnoreTarget(nameof(StockMovement.CurrentStock))]
    [MapperIgnoreTarget(nameof(StockMovement.TotalCost))]
    [MapperIgnoreTarget(nameof(StockMovement.CreatedByUser))]
    [MapperIgnoreTarget(nameof(StockMovement.CreatedByUserId))]
    public partial StockMovement CreateMovementDtoToMovement(CreateStockMovementDto dto);
    
    // Mapeamento customizado para incluir nomes
    public StockMovementDto MapWithDetails(StockMovement movement)
    {
        var dto = MovementToMovementDto(movement);
        dto.ProductName = movement.Product?.Name ?? "";
        dto.ProductSku = movement.Product?.Sku ?? "";
        dto.WarehouseName = movement.Warehouse?.Name;
        dto.TypeName = movement.Type.ToString();
        dto.ReasonName = movement.Reason.ToString();
        dto.CreatedByUserName = movement.CreatedByUser?.UserName ?? "";
        return dto;
    }
}
