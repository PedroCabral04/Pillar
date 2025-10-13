using Microsoft.EntityFrameworkCore;
using erp.Data;
using erp.DTOs.Inventory;
using erp.Models.Inventory;
using erp.Mappings;

namespace erp.Services.Inventory;

public class StockMovementService : IStockMovementService
{
    private readonly ApplicationDbContext _context;
    private readonly StockMovementMapper _mapper;

    public StockMovementService(ApplicationDbContext context, StockMovementMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<StockMovementDto> CreateMovementAsync(CreateStockMovementDto dto, int userId)
    {
        var product = await _context.Products.FindAsync(dto.ProductId);
        if (product == null)
        {
            throw new InvalidOperationException("Produto não encontrado");
        }

        // Validar estoque para saídas
        if (dto.Type == (int)MovementType.Out && !product.AllowNegativeStock)
        {
            if (product.CurrentStock < dto.Quantity)
            {
                throw new InvalidOperationException(
                    $"Estoque insuficiente. Disponível: {product.CurrentStock}, Solicitado: {dto.Quantity}");
            }
        }

        var movement = _mapper.CreateMovementDtoToMovement(dto);
        movement.CreatedByUserId = userId;
        movement.PreviousStock = product.CurrentStock;
        movement.TotalCost = dto.Quantity * dto.UnitCost;

        // Atualizar estoque do produto
        switch ((MovementType)dto.Type)
        {
            case MovementType.In:
                product.CurrentStock += dto.Quantity;
                break;
            case MovementType.Out:
                product.CurrentStock -= dto.Quantity;
                break;
            case MovementType.Transfer:
                // Para transferências, a lógica seria mais complexa
                // Por enquanto, não altera o estoque total
                break;
        }

        movement.CurrentStock = product.CurrentStock;
        product.UpdatedAt = DateTime.UtcNow;

        _context.StockMovements.Add(movement);
        await _context.SaveChangesAsync();

        return await GetMovementByIdAsync(movement.Id)
            ?? throw new InvalidOperationException("Erro ao criar movimentação");
    }

    public async Task<StockMovementDto> CreateEntryAsync(
        int productId, 
        decimal quantity, 
        decimal unitCost, 
        string? documentNumber, 
        string? notes, 
        int userId, 
        int? warehouseId = null)
    {
        var dto = new CreateStockMovementDto
        {
            ProductId = productId,
            Type = (int)MovementType.In,
            Reason = (int)MovementReason.Purchase,
            Quantity = quantity,
            UnitCost = unitCost,
            DocumentNumber = documentNumber,
            Notes = notes,
            WarehouseId = warehouseId,
            MovementDate = DateTime.UtcNow
        };

        return await CreateMovementAsync(dto, userId);
    }

    public async Task<StockMovementDto> CreateExitAsync(
        int productId, 
        decimal quantity, 
        string? documentNumber, 
        string? notes, 
        int userId, 
        int? warehouseId = null)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            throw new InvalidOperationException("Produto não encontrado");
        }

        var dto = new CreateStockMovementDto
        {
            ProductId = productId,
            Type = (int)MovementType.Out,
            Reason = (int)MovementReason.Sale,
            Quantity = quantity,
            UnitCost = product.CostPrice,
            DocumentNumber = documentNumber,
            Notes = notes,
            WarehouseId = warehouseId,
            MovementDate = DateTime.UtcNow
        };

        return await CreateMovementAsync(dto, userId);
    }

    public async Task<StockMovementDto> CreateAdjustmentAsync(
        int productId, 
        decimal newStock, 
        string reason, 
        int userId, 
        int? warehouseId = null)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            throw new InvalidOperationException("Produto não encontrado");
        }

        var difference = newStock - product.CurrentStock;
        var movementType = difference >= 0 ? MovementType.In : MovementType.Out;
        var quantity = Math.Abs(difference);

        var dto = new CreateStockMovementDto
        {
            ProductId = productId,
            Type = (int)movementType,
            Reason = (int)MovementReason.Adjustment,
            Quantity = quantity,
            UnitCost = product.CostPrice,
            Notes = $"Ajuste de estoque: {reason}. Estoque anterior: {product.CurrentStock}, Novo estoque: {newStock}",
            WarehouseId = warehouseId,
            MovementDate = DateTime.UtcNow
        };

        return await CreateMovementAsync(dto, userId);
    }

    public async Task<IEnumerable<StockMovementDto>> GetMovementsByProductAsync(
        int productId, 
        DateTime? startDate = null, 
        DateTime? endDate = null)
    {
        var query = _context.StockMovements
            .Include(m => m.Product)
            .Include(m => m.Warehouse)
            .Include(m => m.CreatedByUser)
            .Where(m => m.ProductId == productId);

        if (startDate.HasValue)
        {
            query = query.Where(m => m.MovementDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(m => m.MovementDate <= endDate.Value);
        }

        var movements = await query
            .OrderByDescending(m => m.MovementDate)
            .ToListAsync();

        return movements.Select(m => _mapper.MapWithDetails(m));
    }

    public async Task<IEnumerable<StockMovementDto>> GetMovementsByDateRangeAsync(
        DateTime startDate, 
        DateTime endDate, 
        int? warehouseId = null)
    {
        var query = _context.StockMovements
            .Include(m => m.Product)
            .Include(m => m.Warehouse)
            .Include(m => m.CreatedByUser)
            .Where(m => m.MovementDate >= startDate && m.MovementDate <= endDate);

        if (warehouseId.HasValue)
        {
            query = query.Where(m => m.WarehouseId == warehouseId.Value);
        }

        var movements = await query
            .OrderByDescending(m => m.MovementDate)
            .ToListAsync();

        return movements.Select(m => _mapper.MapWithDetails(m));
    }

    public async Task<StockMovementDto?> GetMovementByIdAsync(int id)
    {
        var movement = await _context.StockMovements
            .Include(m => m.Product)
            .Include(m => m.Warehouse)
            .Include(m => m.CreatedByUser)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (movement == null) return null;

        return _mapper.MapWithDetails(movement);
    }
}
