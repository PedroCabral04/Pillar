using Microsoft.EntityFrameworkCore;
using erp.Data;
using erp.DTOs.Inventory;
using erp.Models.Inventory;
using erp.Mappings;

namespace erp.Services.Inventory;

public class StockCountService : IStockCountService
{
    private readonly ApplicationDbContext _context;
    private readonly StockCountMapper _mapper;
    private readonly IStockMovementService _movementService;

    public StockCountService(
        ApplicationDbContext context, 
        StockCountMapper mapper,
        IStockMovementService movementService)
    {
        _context = context;
        _mapper = mapper;
        _movementService = movementService;
    }

    public async Task<(IEnumerable<StockCountDto> Counts, int TotalCount)> GetCountsAsync(
        string? status = null,
        int? warehouseId = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = _context.StockCounts
            .Include(c => c.Warehouse)
            .Include(c => c.CreatedByUser)
            .Include(c => c.ApprovedByUser)
            .Include(c => c.Items)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<StockCountStatus>(status, out var statusEnum))
            {
                query = query.Where(c => c.Status == statusEnum);
            }
        }

        if (warehouseId.HasValue)
        {
            query = query.Where(c => c.WarehouseId == warehouseId.Value);
        }

        var totalCount = await query.CountAsync();

        var counts = await query
            .OrderByDescending(c => c.CountDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (counts.Select(c => _mapper.MapWithDetails(c)), totalCount);
    }

    public async Task<StockCountDto> CreateCountAsync(CreateStockCountDto dto, int userId)
    {
        var count = _mapper.CreateCountDtoToCount(dto);
        
        if (string.IsNullOrWhiteSpace(count.CountNumber))
        {
            count.CountNumber = await GenerateCountNumberAsync();
        }

        count.CreatedByUserId = userId;
        count.Status = StockCountStatus.InProgress;
        count.CountDate = dto.CountDate;

        _context.StockCounts.Add(count);
        await _context.SaveChangesAsync();

        return await GetCountByIdAsync(count.Id)
            ?? throw new InvalidOperationException("Erro ao criar contagem");
    }

    public async Task<StockCountDto?> GetCountByIdAsync(int id)
    {
        var count = await _context.StockCounts
            .Include(c => c.Warehouse)
            .Include(c => c.CreatedByUser)
            .Include(c => c.ApprovedByUser)
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (count == null) return null;

        return _mapper.MapWithDetails(count);
    }

    public async Task<IEnumerable<StockCountDto>> GetActiveCountsAsync()
    {
        var counts = await _context.StockCounts
            .Include(c => c.Warehouse)
            .Include(c => c.CreatedByUser)
            .Include(c => c.Items)
            .Where(c => c.Status == StockCountStatus.InProgress || c.Status == StockCountStatus.Pending)
            .OrderByDescending(c => c.CountDate)
            .ToListAsync();

        return counts.Select(c => _mapper.MapWithDetails(c));
    }

    public async Task<StockCountDto> AddItemToCountAsync(AddStockCountItemDto dto)
    {
        var count = await _context.StockCounts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == dto.StockCountId);

        if (count == null)
        {
            throw new InvalidOperationException("Contagem não encontrada");
        }

        if (count.Status != StockCountStatus.InProgress)
        {
            throw new InvalidOperationException("Não é possível adicionar itens a uma contagem que não está em progresso");
        }

        var product = await _context.Products.FindAsync(dto.ProductId);
        if (product == null)
        {
            throw new InvalidOperationException("Produto não encontrado");
        }

        // Verificar se o produto já foi contado
        var existingItem = count.Items.FirstOrDefault(i => i.ProductId == dto.ProductId);
        if (existingItem != null)
        {
            // Atualizar contagem existente
            existingItem.PhysicalStock = dto.PhysicalStock;
            existingItem.Notes = dto.Notes;
        }
        else
        {
            // Adicionar novo item
            var item = _mapper.AddItemDtoToItem(dto);
            item.SystemStock = product.CurrentStock;
            count.Items.Add(item);
        }

        await _context.SaveChangesAsync();

        return await GetCountByIdAsync(count.Id)
            ?? throw new InvalidOperationException("Erro ao adicionar item à contagem");
    }

    public async Task<StockCountDto> ApproveCountAsync(ApproveStockCountDto dto, int userId)
    {
        var count = await _context.StockCounts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.Id == dto.StockCountId);

        if (count == null)
        {
            throw new InvalidOperationException("Contagem não encontrada");
        }

        if (count.Status == StockCountStatus.Approved)
        {
            throw new InvalidOperationException("Esta contagem já foi aprovada");
        }

        if (count.Status == StockCountStatus.Cancelled)
        {
            throw new InvalidOperationException("Não é possível aprovar uma contagem cancelada");
        }

        // Aplicar ajustes de estoque se solicitado
        if (dto.ApplyAdjustments)
        {
            foreach (var item in count.Items)
            {
                if (item.PhysicalStock != item.SystemStock)
                {
                    await _movementService.CreateAdjustmentAsync(
                        item.ProductId,
                        item.PhysicalStock,
                        $"Ajuste de inventário {count.CountNumber}",
                        userId,
                        count.WarehouseId
                    );
                }
            }
        }

        count.Status = StockCountStatus.Approved;
        count.ApprovedByUserId = userId;
        count.ClosedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetCountByIdAsync(count.Id)
            ?? throw new InvalidOperationException("Erro ao aprovar contagem");
    }

    public async Task<bool> CancelCountAsync(int countId)
    {
        var count = await _context.StockCounts.FindAsync(countId);
        if (count == null) return false;

        if (count.Status == StockCountStatus.Approved)
        {
            throw new InvalidOperationException("Não é possível cancelar uma contagem já aprovada");
        }

        count.Status = StockCountStatus.Cancelled;
        count.ClosedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<string> GenerateCountNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var lastCount = await _context.StockCounts
            .Where(c => c.CountNumber.StartsWith($"INV-{year}-"))
            .OrderByDescending(c => c.CountNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastCount != null)
        {
            var lastNumberPart = lastCount.CountNumber.Split('-').LastOrDefault();
            if (int.TryParse(lastNumberPart, out int lastNum))
            {
                nextNumber = lastNum + 1;
            }
        }

        return $"INV-{year}-{nextNumber:D4}";
    }
}
