using Microsoft.EntityFrameworkCore;
using erp.Data;
using erp.DTOs.Inventory;
using erp.Models.Inventory;
using erp.Mappings;

namespace erp.Services.Inventory;

public class InventoryService : IInventoryService
{
    private readonly ApplicationDbContext _context;
    private readonly ProductMapper _productMapper;

    public InventoryService(ApplicationDbContext context, ProductMapper productMapper)
    {
        _context = context;
        _productMapper = productMapper;
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Images)
            .Include(p => p.Suppliers)
            .Include(p => p.CreatedByUser)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return null;

        return _productMapper.MapWithCalculations(product);
    }

    public async Task<ProductDto?> GetProductBySkuAsync(string sku)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Images)
            .Include(p => p.Suppliers)
            .Include(p => p.CreatedByUser)
            .FirstOrDefaultAsync(p => p.Sku == sku);

        if (product == null) return null;

        return _productMapper.MapWithCalculations(product);
    }

    public async Task<(IEnumerable<ProductDto> Products, int TotalCount)> SearchProductsAsync(ProductSearchDto search)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.CreatedByUser)
            .AsQueryable();

        // Filtros
        if (!string.IsNullOrWhiteSpace(search.SearchTerm))
        {
            var term = search.SearchTerm.ToLower();
            query = query.Where(p => 
                p.Name.ToLower().Contains(term) ||
                p.Sku.ToLower().Contains(term) ||
                (p.Barcode != null && p.Barcode.ToLower().Contains(term)));
        }

        if (search.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == search.CategoryId.Value);
        }

        if (search.BrandId.HasValue)
        {
            query = query.Where(p => p.BrandId == search.BrandId.Value);
        }

        if (search.Status.HasValue)
        {
            query = query.Where(p => (int)p.Status == search.Status.Value);
        }

        if (search.IsActive.HasValue)
        {
            query = query.Where(p => p.IsActive == search.IsActive.Value);
        }

        if (search.LowStock == true)
        {
            query = query.Where(p => p.CurrentStock <= p.MinimumStock);
        }

        // Total de registros
        var totalCount = await query.CountAsync();

        // Ordenação
        query = search.SortBy?.ToLower() switch
        {
            "name" => search.SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "sku" => search.SortDescending ? query.OrderByDescending(p => p.Sku) : query.OrderBy(p => p.Sku),
            "price" => search.SortDescending ? query.OrderByDescending(p => p.SalePrice) : query.OrderBy(p => p.SalePrice),
            "stock" => search.SortDescending ? query.OrderByDescending(p => p.CurrentStock) : query.OrderBy(p => p.CurrentStock),
            _ => query.OrderBy(p => p.Name)
        };

        // Paginação
        var products = await query
            .Skip((search.Page - 1) * search.PageSize)
            .Take(search.PageSize)
            .ToListAsync();

        var dtos = products.Select(p => _productMapper.MapWithCalculations(p));

        return (dtos, totalCount);
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto dto, int userId)
    {
        // Validar SKU único
        if (await _context.Products.AnyAsync(p => p.Sku == dto.Sku))
        {
            throw new InvalidOperationException($"Já existe um produto com o SKU '{dto.Sku}'");
        }

        // Validar Barcode único se informado
        if (!string.IsNullOrWhiteSpace(dto.Barcode) && 
            await _context.Products.AnyAsync(p => p.Barcode == dto.Barcode))
        {
            throw new InvalidOperationException($"Já existe um produto com o código de barras '{dto.Barcode}'");
        }

        var product = _productMapper.CreateProductDtoToProduct(dto);
        product.CreatedByUserId = userId;
        product.CreatedAt = DateTime.UtcNow;

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return await GetProductByIdAsync(product.Id) 
            ?? throw new InvalidOperationException("Erro ao criar produto");
    }

    public async Task<ProductDto> UpdateProductAsync(UpdateProductDto dto)
    {
        var product = await _context.Products.FindAsync(dto.Id);
        if (product == null)
        {
            throw new InvalidOperationException("Produto não encontrado");
        }

        // Validar SKU único
        if (await _context.Products.AnyAsync(p => p.Sku == dto.Sku && p.Id != dto.Id))
        {
            throw new InvalidOperationException($"Já existe outro produto com o SKU '{dto.Sku}'");
        }

        // Validar Barcode único se informado
        if (!string.IsNullOrWhiteSpace(dto.Barcode) && 
            await _context.Products.AnyAsync(p => p.Barcode == dto.Barcode && p.Id != dto.Id))
        {
            throw new InvalidOperationException($"Já existe outro produto com o código de barras '{dto.Barcode}'");
        }

        _productMapper.UpdateProductDtoToProduct(dto, product);
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetProductByIdAsync(product.Id) 
            ?? throw new InvalidOperationException("Erro ao atualizar produto");
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return false;

        // Verificar se há movimentações
        var hasMovements = await _context.StockMovements.AnyAsync(m => m.ProductId == id);
        if (hasMovements)
        {
            throw new InvalidOperationException("Não é possível excluir um produto com movimentações de estoque");
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> BulkUpdatePricesAsync(BulkUpdatePriceDto dto)
    {
        var products = await _context.Products
            .Where(p => dto.ProductIds.Contains(p.Id))
            .ToListAsync();

        foreach (var product in products)
        {
            if (dto.CostPriceAdjustment.HasValue)
            {
                if (dto.AdjustmentIsPercentage)
                {
                    product.CostPrice += product.CostPrice * (dto.CostPriceAdjustment.Value / 100);
                }
                else
                {
                    product.CostPrice += dto.CostPriceAdjustment.Value;
                }
            }

            if (dto.SalePriceAdjustment.HasValue)
            {
                if (dto.AdjustmentIsPercentage)
                {
                    product.SalePrice += product.SalePrice * (dto.SalePriceAdjustment.Value / 100);
                }
                else
                {
                    product.SalePrice += dto.SalePriceAdjustment.Value;
                }
            }

            product.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<ProductCategoryDto>> GetCategoriesAsync()
    {
        var categories = await _context.ProductCategories
            .Include(c => c.ParentCategory)
            .Include(c => c.SubCategories)
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return _productMapper.CategoriesToCategoryDtos(categories);
    }

    public async Task<ProductCategoryDto?> GetCategoryByIdAsync(int id)
    {
        var category = await _context.ProductCategories
            .Include(c => c.ParentCategory)
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null) return null;

        return _productMapper.CategoryToCategoryDto(category);
    }

    public async Task<decimal> GetProductStockAsync(int productId, int? warehouseId = null)
    {
        var product = await _context.Products.FindAsync(productId);
        return product?.CurrentStock ?? 0;
    }

    public async Task<bool> ValidateStockAvailabilityAsync(int productId, decimal quantity, int? warehouseId = null)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null) return false;

        if (product.AllowNegativeStock) return true;

        return product.CurrentStock >= quantity;
    }

    public async Task<IEnumerable<StockAlertDto>> GetStockAlertsAsync()
    {
        var alerts = new List<StockAlertDto>();

        alerts.AddRange(await GetLowStockProductsAsync());
        alerts.AddRange(await GetOverstockProductsAsync());

        return alerts;
    }

    public async Task<IEnumerable<StockAlertDto>> GetLowStockProductsAsync()
    {
        var products = await _context.Products
            .Where(p => p.IsActive && p.CurrentStock <= p.ReorderPoint)
            .Select(p => new StockAlertDto
            {
                ProductId = p.Id,
                ProductName = p.Name,
                ProductSku = p.Sku,
                CurrentStock = p.CurrentStock,
                MinimumStock = p.MinimumStock,
                ReorderPoint = p.ReorderPoint,
                AlertType = p.CurrentStock <= p.MinimumStock ? "Critical" : "Low",
                AlertLevel = p.CurrentStock <= p.MinimumStock ? "Error" : "Warning",
                LastMovementDate = _context.StockMovements
                    .Where(m => m.ProductId == p.Id)
                    .OrderByDescending(m => m.MovementDate)
                    .Select(m => m.MovementDate)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return products;
    }

    public async Task<IEnumerable<StockAlertDto>> GetOverstockProductsAsync()
    {
        var products = await _context.Products
            .Where(p => p.IsActive && p.MaximumStock > 0 && p.CurrentStock > p.MaximumStock)
            .Select(p => new StockAlertDto
            {
                ProductId = p.Id,
                ProductName = p.Name,
                ProductSku = p.Sku,
                CurrentStock = p.CurrentStock,
                MinimumStock = p.MinimumStock,
                ReorderPoint = p.ReorderPoint,
                AlertType = "Overstock",
                AlertLevel = "Info",
                LastMovementDate = _context.StockMovements
                    .Where(m => m.ProductId == p.Id)
                    .OrderByDescending(m => m.MovementDate)
                    .Select(m => m.MovementDate)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return products;
    }

    public async Task<IEnumerable<StockAlertDto>> GetInactiveProductsAsync(int daysInactive = 90)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysInactive);

        var products = await _context.Products
            .Where(p => p.IsActive)
            .Where(p => !_context.StockMovements
                .Any(m => m.ProductId == p.Id && m.MovementDate >= cutoffDate))
            .Select(p => new StockAlertDto
            {
                ProductId = p.Id,
                ProductName = p.Name,
                ProductSku = p.Sku,
                CurrentStock = p.CurrentStock,
                MinimumStock = p.MinimumStock,
                ReorderPoint = p.ReorderPoint,
                AlertType = "Inactive",
                AlertLevel = "Warning",
                LastMovementDate = _context.StockMovements
                    .Where(m => m.ProductId == p.Id)
                    .OrderByDescending(m => m.MovementDate)
                    .Select(m => m.MovementDate)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return products;
    }

    public async Task<(IEnumerable<ProductCategoryDto> Categories, int TotalCount)> GetCategoriesAsync(
        string? search = null, 
        int? parentCategoryId = null, 
        bool? isActive = null, 
        int page = 1, 
        int pageSize = 20)
    {
        var query = _context.ProductCategories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(searchLower) || c.Code.ToLower().Contains(searchLower));
        }

        if (parentCategoryId.HasValue)
        {
            query = query.Where(c => c.ParentCategoryId == parentCategoryId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync();

        var categories = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ProductCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                ParentCategoryId = c.ParentCategoryId,
                ParentCategoryName = c.ParentCategory != null ? c.ParentCategory.Name : null,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                SubCategories = new List<ProductCategoryDto>()
            })
            .ToListAsync();

        return (categories, totalCount);
    }

    public async Task<ProductCategoryDto> CreateCategoryAsync(CreateProductCategoryDto dto)
    {
        var category = new ProductCategory
        {
            Name = dto.Name,
            Code = dto.Code,
            ParentCategoryId = dto.ParentCategoryId,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProductCategories.Add(category);
        await _context.SaveChangesAsync();

        return await GetCategoryByIdAsync(category.Id) ?? throw new InvalidOperationException("Falha ao criar categoria");
    }

    public async Task<ProductCategoryDto> UpdateCategoryAsync(UpdateProductCategoryDto dto)
    {
        var category = await _context.ProductCategories.FindAsync(dto.Id);
        if (category == null)
            throw new InvalidOperationException($"Categoria com ID {dto.Id} não encontrada");

        category.Name = dto.Name;
        category.Code = dto.Code;
        category.ParentCategoryId = dto.ParentCategoryId;
        category.IsActive = dto.IsActive;
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetCategoryByIdAsync(category.Id) ?? throw new InvalidOperationException("Falha ao atualizar categoria");
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        var category = await _context.ProductCategories
            .Include(c => c.SubCategories)
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null) return false;

        if (category.SubCategories.Any())
            throw new InvalidOperationException("Não é possível deletar categoria que possui subcategorias");

        if (category.Products.Any())
            throw new InvalidOperationException("Não é possível deletar categoria que possui produtos vinculados");

        _context.ProductCategories.Remove(category);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<(IEnumerable<StockMovementDto> Movements, int TotalCount)> GetStockMovementsAsync(
        int? productId = null,
        string? movementType = null,
        int? warehouseId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = _context.StockMovements
            .Include(m => m.Product)
            .Include(m => m.CreatedByUser)
            .AsQueryable();

        if (productId.HasValue)
            query = query.Where(m => m.ProductId == productId.Value);

        if (warehouseId.HasValue)
            query = query.Where(m => m.WarehouseId == warehouseId.Value);

        if (startDate.HasValue)
            query = query.Where(m => m.MovementDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(m => m.MovementDate <= endDate.Value);

        var totalCount = await query.CountAsync();

        var movements = await query
            .OrderByDescending(m => m.MovementDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new StockMovementDto
            {
                Id = m.Id,
                ProductId = m.ProductId,
                ProductName = m.Product.Name,
                ProductSku = m.Product.Sku,
                Type = (int)m.Type,
                TypeName = m.Type.ToString(),
                Reason = (int)m.Reason,
                ReasonName = m.Reason.ToString(),
                Quantity = m.Quantity,
                PreviousStock = m.PreviousStock,
                CurrentStock = m.CurrentStock,
                UnitCost = m.UnitCost,
                TotalCost = m.TotalCost,
                MovementDate = m.MovementDate,
                Notes = m.Notes,
                CreatedByUserId = m.CreatedByUserId,
                CreatedByUserName = m.CreatedByUser.UserName ?? "Unknown",
                DocumentNumber = m.DocumentNumber,
                WarehouseId = m.WarehouseId,
                SaleOrderId = m.SaleOrderId,
                PurchaseOrderId = m.PurchaseOrderId,
                TransferId = m.TransferId
            })
            .ToListAsync();

        return (movements, totalCount);
    }

    public async Task<StockMovementDto?> GetStockMovementByIdAsync(int id)
    {
        var movement = await _context.StockMovements
            .Include(m => m.Product)
            .Include(m => m.CreatedByUser)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (movement == null) return null;

        return new StockMovementDto
        {
            Id = movement.Id,
            ProductId = movement.ProductId,
            ProductName = movement.Product.Name,
            ProductSku = movement.Product.Sku,
            Type = (int)movement.Type,
            TypeName = movement.Type.ToString(),
            Reason = (int)movement.Reason,
            ReasonName = movement.Reason.ToString(),
            Quantity = movement.Quantity,
            PreviousStock = movement.PreviousStock,
            CurrentStock = movement.CurrentStock,
            UnitCost = movement.UnitCost,
            TotalCost = movement.TotalCost,
            MovementDate = movement.MovementDate,
            Notes = movement.Notes,
            CreatedByUserId = movement.CreatedByUserId,
            CreatedByUserName = movement.CreatedByUser.UserName ?? "Unknown",
            DocumentNumber = movement.DocumentNumber,
            WarehouseId = movement.WarehouseId,
            SaleOrderId = movement.SaleOrderId,
            PurchaseOrderId = movement.PurchaseOrderId,
            TransferId = movement.TransferId
        };
    }

    public async Task<StockMovementDto> CreateStockMovementAsync(CreateStockMovementDto dto, int userId)
    {
        var product = await _context.Products.FindAsync(dto.ProductId);
        if (product == null)
            throw new InvalidOperationException($"Produto com ID {dto.ProductId} não encontrado");

        var previousStock = product.CurrentStock;
        
        // Convert int to enum
        var movementType = (MovementType)dto.Type;
        var movementReason = (MovementReason)dto.Reason;
        
        var newStock = movementType switch
        {
            MovementType.In => previousStock + dto.Quantity,
            MovementType.Out => previousStock - dto.Quantity,
            MovementType.Transfer => previousStock - dto.Quantity,
            _ => throw new InvalidOperationException($"Tipo de movimentação inválido")
        };

        if (newStock < 0 && !product.AllowNegativeStock)
            throw new InvalidOperationException("Estoque não pode ficar negativo");

        var movement = new StockMovement
        {
            ProductId = dto.ProductId,
            Type = movementType,
            Reason = movementReason,
            Quantity = dto.Quantity,
            PreviousStock = previousStock,
            CurrentStock = newStock,
            UnitCost = dto.UnitCost,
            TotalCost = dto.Quantity * dto.UnitCost,
            MovementDate = dto.MovementDate,
            Notes = dto.Notes,
            CreatedByUserId = userId,
            DocumentNumber = dto.DocumentNumber,
            WarehouseId = dto.WarehouseId,
            SaleOrderId = dto.SaleOrderId,
            PurchaseOrderId = dto.PurchaseOrderId,
            TransferId = dto.TransferId
        };

        product.CurrentStock = newStock;

        _context.StockMovements.Add(movement);
        await _context.SaveChangesAsync();

        return await GetStockMovementByIdAsync(movement.Id) ?? throw new InvalidOperationException("Falha ao criar movimentação");
    }

    public async Task<(IEnumerable<WarehouseDto> Warehouses, int TotalCount)> GetWarehousesAsync(
        bool? isActive = null,
        int page = 1,
        int pageSize = 100)
    {
        var query = _context.Warehouses.AsQueryable();

        if (isActive.HasValue)
            query = query.Where(w => w.IsActive == isActive.Value);

        var totalCount = await query.CountAsync();

        var warehouses = await query
            .OrderBy(w => w.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new WarehouseDto
            {
                Id = w.Id,
                Name = w.Name,
                Code = w.Code,
                Address = w.Address,
                IsActive = w.IsActive,
                CreatedAt = w.CreatedAt,
                UpdatedAt = w.UpdatedAt
            })
            .ToListAsync();

        return (warehouses, totalCount);
    }
}
