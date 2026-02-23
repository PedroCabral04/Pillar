using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using erp.Data;
using erp.DTOs.Inventory;
using erp.Models.Inventory;
using erp.Mappings;
using System.Globalization;
using System.Text;

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

    public async Task<ProductImportResultDto> ImportProductsFromExcelAsync(
        Stream fileStream,
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (fileStream == null || !fileStream.CanRead)
        {
            throw new InvalidOperationException("Arquivo invalido para importacao.");
        }

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage(fileStream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();

        if (worksheet?.Dimension == null)
        {
            throw new InvalidOperationException("A planilha esta vazia.");
        }

        var headerMap = BuildHeaderMap(worksheet);
        if (!headerMap.TryGetValue("nome", out var nameCol) ||
            !headerMap.TryGetValue("categoria", out var categoryCol) ||
            !headerMap.TryGetValue("preco_venda", out var salePriceCol))
        {
            throw new InvalidOperationException("Colunas obrigatorias ausentes. Use: Nome, Categoria, Preco de Venda.");
        }

        headerMap.TryGetValue("sku", out var skuCol);
        headerMap.TryGetValue("preco_custo", out var costPriceCol);
        headerMap.TryGetValue("estoque_inicial", out var stockCol);
        headerMap.TryGetValue("unidade", out var unitCol);
        headerMap.TryGetValue("codigo_barras", out var barcodeCol);

        var categories = await _context.ProductCategories
            .AsNoTracking()
            .Select(c => new { c.Id, c.Name, c.Code })
            .ToListAsync(cancellationToken);

        var categoryById = categories.ToDictionary(c => c.Id, c => c.Id);
        var categoryByNameOrCode = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var category in categories)
        {
            categoryByNameOrCode[NormalizeKey(category.Name)] = category.Id;
            categoryByNameOrCode[NormalizeKey(category.Code)] = category.Id;
        }

        await ImportCategoriesFromTemplateAsync(
            package,
            categoryById,
            categoryByNameOrCode,
            cancellationToken);

        var existingSkuList = await _context.Products
            .AsNoTracking()
            .Select(p => p.Sku)
            .ToListAsync(cancellationToken);

        var existingBarcodeList = await _context.Products
            .AsNoTracking()
            .Where(p => p.Barcode != null && p.Barcode != string.Empty)
            .Select(p => p.Barcode!)
            .ToListAsync(cancellationToken);

        var existingSkus = new HashSet<string>(existingSkuList, StringComparer.OrdinalIgnoreCase);
        var existingBarcodes = new HashSet<string>(existingBarcodeList, StringComparer.OrdinalIgnoreCase);
        var seenInputSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenInputBarcodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pendingProducts = new List<PendingImportProduct>(200);

        var result = new ProductImportResultDto();
        var firstRow = worksheet.Dimension.Start.Row;
        var lastRow = worksheet.Dimension.End.Row;

        for (var row = firstRow + 1; row <= lastRow; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var rowValues = new[]
            {
                GetCellText(worksheet, row, nameCol),
                GetCellText(worksheet, row, categoryCol),
                GetCellText(worksheet, row, salePriceCol),
                skuCol > 0 ? GetCellText(worksheet, row, skuCol) : null,
                barcodeCol > 0 ? GetCellText(worksheet, row, barcodeCol) : null,
                unitCol > 0 ? GetCellText(worksheet, row, unitCol) : null,
                stockCol > 0 ? GetCellText(worksheet, row, stockCol) : null,
                costPriceCol > 0 ? GetCellText(worksheet, row, costPriceCol) : null,
            };

            if (rowValues.All(v => string.IsNullOrWhiteSpace(v)))
            {
                continue;
            }

            result.TotalRows++;

            var inputSku = skuCol > 0 ? GetCellText(worksheet, row, skuCol) : null;
            var name = GetCellText(worksheet, row, nameCol);
            var categoryRaw = GetCellText(worksheet, row, categoryCol);
            var salePriceRaw = GetCellText(worksheet, row, salePriceCol);
            var costPriceRaw = costPriceCol > 0 ? GetCellText(worksheet, row, costPriceCol) : null;
            var stockRaw = stockCol > 0 ? GetCellText(worksheet, row, stockCol) : null;
            var unitRaw = unitCol > 0 ? GetCellText(worksheet, row, unitCol) : null;
            var barcodeRaw = barcodeCol > 0 ? GetCellText(worksheet, row, barcodeCol) : null;

            if (string.IsNullOrWhiteSpace(name))
            {
                AddIssue(result, row, "Nome obrigatorio.", inputSku, name, isSkipped: false);
                continue;
            }

            if (string.IsNullOrWhiteSpace(categoryRaw))
            {
                AddIssue(result, row, "Categoria obrigatoria.", inputSku, name, isSkipped: false);
                continue;
            }

            if (!TryResolveCategoryId(categoryRaw, categoryById, categoryByNameOrCode, out var categoryId))
            {
                AddIssue(result, row, $"Categoria '{categoryRaw}' nao encontrada.", inputSku, name, isSkipped: false);
                continue;
            }

            if (!TryParseDecimal(salePriceRaw, out var salePrice) || salePrice <= 0)
            {
                AddIssue(result, row, "Preco de venda invalido.", inputSku, name, isSkipped: false);
                continue;
            }

            var costPrice = salePrice;
            if (!string.IsNullOrWhiteSpace(costPriceRaw) && (!TryParseDecimal(costPriceRaw, out costPrice) || costPrice < 0))
            {
                AddIssue(result, row, "Preco de custo invalido.", inputSku, name, isSkipped: false);
                continue;
            }

            var initialStock = 0m;
            if (!string.IsNullOrWhiteSpace(stockRaw) && (!TryParseDecimal(stockRaw, out initialStock) || initialStock < 0))
            {
                AddIssue(result, row, "Estoque inicial invalido.", inputSku, name, isSkipped: false);
                continue;
            }

            var normalizedInputSku = string.IsNullOrWhiteSpace(inputSku) ? null : inputSku.Trim();
            if (!string.IsNullOrWhiteSpace(normalizedInputSku))
            {
                if (!seenInputSkus.Add(normalizedInputSku))
                {
                    AddIssue(result, row, $"SKU '{normalizedInputSku}' duplicado na planilha.", normalizedInputSku, name, isSkipped: true);
                    continue;
                }

                if (existingSkus.Contains(normalizedInputSku))
                {
                    AddIssue(result, row, $"SKU '{normalizedInputSku}' ja existe no cadastro.", normalizedInputSku, name, isSkipped: true);
                    continue;
                }
            }

            var normalizedBarcode = string.IsNullOrWhiteSpace(barcodeRaw) ? null : barcodeRaw.Trim();
            if (!string.IsNullOrWhiteSpace(normalizedBarcode))
            {
                if (!seenInputBarcodes.Add(normalizedBarcode))
                {
                    AddIssue(result, row, $"Codigo de barras '{normalizedBarcode}' duplicado na planilha.", normalizedInputSku, name, isSkipped: false);
                    continue;
                }

                if (existingBarcodes.Contains(normalizedBarcode))
                {
                    AddIssue(result, row, $"Ja existe um produto com o codigo de barras '{normalizedBarcode}'.", normalizedInputSku, name, isSkipped: false);
                    continue;
                }
            }

            var skuToCreate = normalizedInputSku;
            if (string.IsNullOrWhiteSpace(skuToCreate))
            {
                skuToCreate = GenerateUniqueSku(existingSkus);
            }

            existingSkus.Add(skuToCreate);

            var dto = new CreateProductDto
            {
                Sku = skuToCreate,
                Barcode = normalizedBarcode,
                Name = name.Trim(),
                CategoryId = categoryId,
                SalePrice = salePrice,
                CostPrice = costPrice,
                CurrentStock = initialStock,
                Unit = string.IsNullOrWhiteSpace(unitRaw) ? "UN" : unitRaw.Trim(),
                UnitsPerBox = 1,
                IsActive = true,
                Status = 0
            };

            var product = _productMapper.CreateProductDtoToProduct(dto);
            product.CreatedByUserId = userId;
            product.CreatedAt = DateTime.UtcNow;

            pendingProducts.Add(new PendingImportProduct
            {
                RowNumber = row,
                Product = product
            });

            if (pendingProducts.Count >= 200)
            {
                await FlushImportBatchAsync(
                    pendingProducts,
                    result,
                    existingSkus,
                    existingBarcodes,
                    cancellationToken);
            }
        }

        await FlushImportBatchAsync(
            pendingProducts,
            result,
            existingSkus,
            existingBarcodes,
            cancellationToken);

        return result;
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
                SubCategories = c.SubCategories.Select(sc => new ProductCategoryDto 
                { 
                    Id = sc.Id, 
                    Name = sc.Name 
                }).ToList()
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

    public async Task<ProductStatisticsDto> GetProductStatisticsAsync()
    {
        var products = await _context.Products.ToListAsync();
        var categories = await _context.ProductCategories.CountAsync();

        return new ProductStatisticsDto
        {
            TotalProducts = products.Count,
            ActiveProducts = products.Count(p => p.IsActive && p.Status == ProductStatus.Active),
            InactiveProducts = products.Count(p => !p.IsActive || p.Status == ProductStatus.Inactive),
            LowStockProducts = products.Count(p => p.CurrentStock > 0 && p.CurrentStock <= p.MinimumStock),
            OutOfStockProducts = products.Count(p => p.CurrentStock <= 0),
            OverstockProducts = products.Count(p => p.CurrentStock > p.MaximumStock && p.MaximumStock > 0),
            TotalStockValue = products.Sum(p => p.CurrentStock * p.CostPrice),
            TotalCategories = categories
        };
    }

    public async Task<byte[]> GenerateImportTemplateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var categories = await _context.ProductCategories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Code,
                c.IsActive
            })
            .ToListAsync(cancellationToken);

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Produtos");
        var instructionsSheet = package.Workbook.Worksheets.Add("Instrucoes");
        var categoriesSheet = package.Workbook.Worksheets.Add("Categorias");

        var headers = new[]
        {
            "SKU (Opcional)",
            "Nome",
            "Categoria",
            "Preco de Venda",
            "Preco de Custo (Opcional)",
            "Estoque Inicial (Opcional)",
            "Unidade (Opcional)",
            "Codigo de Barras (Opcional)"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
        }

        var firstCategoryName = categories.FirstOrDefault(c => c.IsActive)?.Name
            ?? categories.FirstOrDefault()?.Name
            ?? "Categoria Exemplo";
        var secondCategoryReference = categories.Skip(1).FirstOrDefault(c => c.IsActive)
            ?? categories.Skip(1).FirstOrDefault()
            ?? categories.FirstOrDefault(c => c.IsActive)
            ?? categories.FirstOrDefault();

        worksheet.Cells[2, 1].Value = string.Empty;
        worksheet.Cells[2, 2].Value = "Produto Exemplo";
        worksheet.Cells[2, 3].Value = firstCategoryName;
        worksheet.Cells[2, 4].Value = 199.90m;
        worksheet.Cells[2, 5].Value = 150.00m;
        worksheet.Cells[2, 6].Value = 10;
        worksheet.Cells[2, 7].Value = "UN";
        worksheet.Cells[2, 8].Value = "7890000000000";

        worksheet.Cells[3, 1].Value = "PROD-EXEMPLO";
        worksheet.Cells[3, 2].Value = "Produto com SKU manual";
        worksheet.Cells[3, 3].Value = secondCategoryReference?.Id.ToString() ?? firstCategoryName;
        worksheet.Cells[3, 4].Value = 89.90m;
        worksheet.Cells[3, 5].Value = 60.00m;
        worksheet.Cells[3, 6].Value = 5;
        worksheet.Cells[3, 7].Value = "UN";
        worksheet.Cells[3, 8].Value = string.Empty;

        worksheet.Cells[1, 1, 1, headers.Length].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        worksheet.Cells[1, 1, 1, headers.Length].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
        worksheet.Cells[2, 4, 3, 6].Style.Numberformat.Format = "#,##0.00";
        worksheet.View.FreezePanes(2, 1);
        worksheet.Cells[1, 1, 3, headers.Length].AutoFitColumns();

        instructionsSheet.Cells[1, 1].Value = "Template de Importacao de Produtos";
        instructionsSheet.Cells[1, 1].Style.Font.Bold = true;
        instructionsSheet.Cells[1, 1].Style.Font.Size = 14;
        instructionsSheet.Cells[3, 1].Value = "1) Colunas obrigatorias: Nome, Categoria, Preco de Venda.";
        instructionsSheet.Cells[4, 1].Value = "2) SKU e opcional. Se vazio, sera gerado automaticamente.";
        instructionsSheet.Cells[5, 1].Value = "3) Se um SKU informado ja existir, a linha sera pulada e reportada.";
        instructionsSheet.Cells[6, 1].Value = "4) Categoria aceita ID, Nome ou Codigo (veja aba Categorias).";
        instructionsSheet.Cells[7, 1].Value = "5) Erros de uma linha nao interrompem o processamento das demais.";

        instructionsSheet.Cells[9, 1].Value = "Campo";
        instructionsSheet.Cells[9, 2].Value = "Obrigatorio";
        instructionsSheet.Cells[9, 3].Value = "Exemplo";
        instructionsSheet.Cells[9, 4].Value = "Observacao";

        var fieldRows = new[]
        {
            new[] { "SKU", "Nao", "PRD-1001", "Se vazio, gera automaticamente" },
            new[] { "Nome", "Sim", "Mouse sem fio", "Maximo recomendado: 200 caracteres" },
            new[] { "Categoria", "Sim", "Informatica ou 3", "Pode usar ID, Nome ou Codigo" },
            new[] { "Preco de Venda", "Sim", "199.90", "Maior que zero" },
            new[] { "Preco de Custo", "Nao", "150.00", "Se vazio, usa preco de venda" },
            new[] { "Estoque Inicial", "Nao", "10", "Se vazio, assume 0" },
            new[] { "Unidade", "Nao", "UN", "Se vazio, assume UN" },
            new[] { "Codigo de Barras", "Nao", "7890000000000", "Opcional" }
        };

        for (var i = 0; i < fieldRows.Length; i++)
        {
            var row = 10 + i;
            instructionsSheet.Cells[row, 1].Value = fieldRows[i][0];
            instructionsSheet.Cells[row, 2].Value = fieldRows[i][1];
            instructionsSheet.Cells[row, 3].Value = fieldRows[i][2];
            instructionsSheet.Cells[row, 4].Value = fieldRows[i][3];
        }

        instructionsSheet.Cells[9, 1, 9, 4].Style.Font.Bold = true;
        instructionsSheet.Cells[9, 1, 9, 4].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        instructionsSheet.Cells[9, 1, 9, 4].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
        instructionsSheet.Cells[1, 1, 20, 4].AutoFitColumns();

        categoriesSheet.Cells[1, 1].Value = "ID";
        categoriesSheet.Cells[1, 2].Value = "Nome";
        categoriesSheet.Cells[1, 3].Value = "Codigo";
        categoriesSheet.Cells[1, 4].Value = "Ativa";
        categoriesSheet.Cells[1, 1, 1, 4].Style.Font.Bold = true;
        categoriesSheet.Cells[1, 1, 1, 4].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        categoriesSheet.Cells[1, 1, 1, 4].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

        if (categories.Count == 0)
        {
            categoriesSheet.Cells[2, 1].Value = "Nenhuma categoria cadastrada no momento.";
        }
        else
        {
            for (var i = 0; i < categories.Count; i++)
            {
                var row = i + 2;
                categoriesSheet.Cells[row, 1].Value = categories[i].Id;
                categoriesSheet.Cells[row, 2].Value = categories[i].Name;
                categoriesSheet.Cells[row, 3].Value = categories[i].Code;
                categoriesSheet.Cells[row, 4].Value = categories[i].IsActive ? "Sim" : "Nao";
            }
        }

        categoriesSheet.Cells[1, 1, Math.Max(2, categories.Count + 1), 4].AutoFitColumns();
        categoriesSheet.View.FreezePanes(2, 1);

        return package.GetAsByteArray();
    }

    public async Task<byte[]> ExportProductsAsync(ProductSearchDto search, string format)
    {
        // Buscar todos os produtos sem paginação para exportação
        search.Page = 1;
        search.PageSize = int.MaxValue;
        
        var (products, _) = await SearchProductsAsync(search);
        var productList = products.ToList();

        if (format.ToLower() == "csv")
        {
            return GenerateCsv(productList);
        }
        
        // Default: retorna CSV
        return GenerateCsv(productList);
    }

    private static Dictionary<string, int> BuildHeaderMap(ExcelWorksheet worksheet)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var startCol = worksheet.Dimension!.Start.Column;
        var endCol = worksheet.Dimension.End.Column;
        var headerRow = worksheet.Dimension.Start.Row;

        for (var col = startCol; col <= endCol; col++)
        {
            var raw = GetCellText(worksheet, headerRow, col);
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var normalized = NormalizeKey(raw);
            if (normalized == "sku")
            {
                result["sku"] = col;
            }
            else if (normalized is "nome" or "produto" or "nomeproduto")
            {
                result["nome"] = col;
            }
            else if (normalized is "categoria" or "categoriacodigo")
            {
                result["categoria"] = col;
            }
            else if (normalized is "precovenda" or "precodevenda" or "venda" or "preco")
            {
                result["preco_venda"] = col;
            }
            else if (normalized is "precocusto" or "custo" or "precodecusto")
            {
                result["preco_custo"] = col;
            }
            else if (normalized is "estoque" or "estoqueinicial" or "saldoinicial")
            {
                result["estoque_inicial"] = col;
            }
            else if (normalized is "unidade" or "und")
            {
                result["unidade"] = col;
            }
            else if (normalized is "codigobarras" or "codbarras" or "barcode")
            {
                result["codigo_barras"] = col;
            }
        }

        return result;
    }

    private async Task ImportCategoriesFromTemplateAsync(
        ExcelPackage package,
        IDictionary<int, int> categoryById,
        IDictionary<string, int> categoryByNameOrCode,
        CancellationToken cancellationToken)
    {
        var categoriesSheet = package.Workbook.Worksheets.FirstOrDefault(w =>
            NormalizeKey(w.Name) == "categorias");

        if (categoriesSheet?.Dimension == null)
        {
            return;
        }

        var headerMap = BuildCategoryHeaderMap(categoriesSheet);
        if (!headerMap.TryGetValue("nome", out var nameCol))
        {
            return;
        }

        headerMap.TryGetValue("codigo", out var codeCol);
        headerMap.TryGetValue("ativa", out var activeCol);

        var firstRow = categoriesSheet.Dimension.Start.Row;
        var lastRow = categoriesSheet.Dimension.End.Row;
        var newCategories = new List<ProductCategory>();

        for (var row = firstRow + 1; row <= lastRow; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var name = GetCellText(categoriesSheet, row, nameCol)?.Trim();
            var code = codeCol > 0 ? GetCellText(categoriesSheet, row, codeCol)?.Trim() : null;

            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(code))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(name) || string.Equals(name, "Nenhuma categoria cadastrada no momento.", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var normalizedName = NormalizeKey(name);
            var normalizedCode = NormalizeKey(code);

            if (categoryByNameOrCode.ContainsKey(normalizedName) ||
                (!string.IsNullOrWhiteSpace(normalizedCode) && categoryByNameOrCode.ContainsKey(normalizedCode)))
            {
                continue;
            }

            var category = new ProductCategory
            {
                Name = name,
                Code = string.IsNullOrWhiteSpace(code) ? BuildCategoryCode(name) : code,
                IsActive = ParseCategoryIsActive(activeCol > 0 ? GetCellText(categoriesSheet, row, activeCol) : null),
                CreatedAt = DateTime.UtcNow
            };

            _context.ProductCategories.Add(category);
            newCategories.Add(category);
        }

        if (newCategories.Count == 0)
        {
            return;
        }

        await _context.SaveChangesAsync(cancellationToken);

        foreach (var category in newCategories)
        {
            categoryById[category.Id] = category.Id;
            categoryByNameOrCode[NormalizeKey(category.Name)] = category.Id;
            categoryByNameOrCode[NormalizeKey(category.Code)] = category.Id;
        }
    }

    private static Dictionary<string, int> BuildCategoryHeaderMap(ExcelWorksheet worksheet)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var startCol = worksheet.Dimension!.Start.Column;
        var endCol = worksheet.Dimension.End.Column;
        var headerRow = worksheet.Dimension.Start.Row;

        for (var col = startCol; col <= endCol; col++)
        {
            var raw = GetCellText(worksheet, headerRow, col);
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var normalized = NormalizeKey(raw);
            if (normalized is "nome" or "categoria")
            {
                result["nome"] = col;
            }
            else if (normalized is "codigo" or "categoriacodigo")
            {
                result["codigo"] = col;
            }
            else if (normalized is "ativa" or "ativo")
            {
                result["ativa"] = col;
            }
        }

        return result;
    }

    private static bool ParseCategoryIsActive(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        var normalized = NormalizeKey(raw);
        return normalized is "sim" or "s" or "1" or "true" or "ativo" or "ativa";
    }

    private static string BuildCategoryCode(string name)
    {
        var cleaned = NormalizeKey(name).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return "CAT";
        }

        return cleaned.Length <= 20 ? cleaned : cleaned[..20];
    }

    private static bool TryResolveCategoryId(
        string raw,
        IReadOnlyDictionary<int, int> categoryById,
        IReadOnlyDictionary<string, int> categoryByNameOrCode,
        out int categoryId)
    {
        categoryId = 0;

        if (int.TryParse(raw, out var idFromCell) && categoryById.TryGetValue(idFromCell, out var foundId))
        {
            categoryId = foundId;
            return true;
        }

        var key = NormalizeKey(raw);
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        return categoryByNameOrCode.TryGetValue(key, out categoryId);
    }

    private static bool TryParseDecimal(string? raw, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var text = raw.Trim();

        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
        {
            return true;
        }

        if (decimal.TryParse(text, NumberStyles.Number, new CultureInfo("pt-BR"), out value))
        {
            return true;
        }

        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        text = text.Replace("R$", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        if (text.Contains(',') && text.Contains('.'))
        {
            text = text.Replace(".", string.Empty, StringComparison.Ordinal)
                .Replace(',', '.');
        }
        else if (text.Contains(','))
        {
            text = text.Replace(',', '.');
        }

        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    private static string NormalizeKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(c))
            {
                sb.Append(char.ToLowerInvariant(c));
            }
        }

        return sb.ToString();
    }

    private static string? GetCellText(ExcelWorksheet worksheet, int row, int col)
    {
        return worksheet.Cells[row, col].Text?.Trim();
    }

    private static string GenerateUniqueSku(IReadOnlySet<string> existingSkus)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var suffix = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
            var sku = $"PRD-{stamp}-{suffix}";

            if (!existingSkus.Contains(sku))
            {
                return sku;
            }
        }

        throw new InvalidOperationException("Nao foi possivel gerar um SKU unico para importacao.");
    }

    private static void AddIssue(
        ProductImportResultDto result,
        int row,
        string reason,
        string? sku,
        string? name,
        bool isSkipped)
    {
        result.Issues.Add(new ProductImportIssueDto
        {
            RowNumber = row,
            Reason = reason,
            Sku = sku,
            Name = name,
            IsSkipped = isSkipped
        });

        if (isSkipped)
        {
            result.SkippedCount++;
            return;
        }

        result.FailedCount++;
    }

    private async Task FlushImportBatchAsync(
        List<PendingImportProduct> pendingProducts,
        ProductImportResultDto result,
        HashSet<string> existingSkus,
        HashSet<string> existingBarcodes,
        CancellationToken cancellationToken)
    {
        if (pendingProducts.Count == 0)
        {
            return;
        }

        _context.Products.AddRange(pendingProducts.Select(p => p.Product));

        try
        {
            await _context.SaveChangesAsync(cancellationToken);

            foreach (var pending in pendingProducts)
            {
                result.ImportedCount++;
                if (!string.IsNullOrWhiteSpace(pending.Product.Barcode))
                {
                    existingBarcodes.Add(pending.Product.Barcode);
                }
            }

            pendingProducts.Clear();
            return;
        }
        catch (DbUpdateException)
        {
            foreach (var entry in _context.ChangeTracker.Entries<Product>().Where(e => e.State == EntityState.Added).ToList())
            {
                entry.State = EntityState.Detached;
            }
        }

        foreach (var pending in pendingProducts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _context.Products.Add(pending.Product);
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                result.ImportedCount++;

                if (!string.IsNullOrWhiteSpace(pending.Product.Barcode))
                {
                    existingBarcodes.Add(pending.Product.Barcode);
                }
            }
            catch (DbUpdateException ex)
            {
                if (_context.Entry(pending.Product).State != EntityState.Detached)
                {
                    _context.Entry(pending.Product).State = EntityState.Detached;
                }

                existingSkus.Remove(pending.Product.Sku);

                var reason = BuildPersistenceErrorReason(ex, pending.Product);
                var isSkipped = reason.Contains("SKU", StringComparison.OrdinalIgnoreCase);
                AddIssue(result, pending.RowNumber, reason, pending.Product.Sku, pending.Product.Name, isSkipped);
            }
            catch (Exception ex)
            {
                if (_context.Entry(pending.Product).State != EntityState.Detached)
                {
                    _context.Entry(pending.Product).State = EntityState.Detached;
                }

                existingSkus.Remove(pending.Product.Sku);
                AddIssue(result, pending.RowNumber, $"Falha inesperada: {ex.Message}", pending.Product.Sku, pending.Product.Name, isSkipped: false);
            }
        }

        pendingProducts.Clear();
    }

    private static string BuildPersistenceErrorReason(DbUpdateException ex, Product product)
    {
        var message = ex.InnerException?.Message ?? ex.Message;

        if (message.Contains("sku", StringComparison.OrdinalIgnoreCase))
        {
            return $"SKU '{product.Sku}' ja existe no cadastro.";
        }

        if (!string.IsNullOrWhiteSpace(product.Barcode) &&
            (message.Contains("barcode", StringComparison.OrdinalIgnoreCase)
             || message.Contains("barra", StringComparison.OrdinalIgnoreCase)))
        {
            return $"Ja existe um produto com o codigo de barras '{product.Barcode}'.";
        }

        return "Falha ao salvar produto por conflito de dados.";
    }

    private sealed class PendingImportProduct
    {
        public int RowNumber { get; init; }
        public Product Product { get; init; } = null!;
    }

    private byte[] GenerateCsv(List<ProductDto> products)
    {
        var sb = new System.Text.StringBuilder();
        
        // Header
        sb.AppendLine("SKU;Código de Barras;Nome;Categoria;Unidade;Estoque Atual;Estoque Mínimo;Estoque Máximo;Preço de Custo;Preço de Venda;Margem de Lucro;Status;Ativo");
        
        // Data
        foreach (var p in products)
        {
            var statusText = p.Status switch
            {
                0 => "Ativo",
                1 => "Inativo",
                2 => "Descontinuado",
                _ => "Desconhecido"
            };
            
            sb.AppendLine($"{Escape(p.Sku)};{Escape(p.Barcode)};{Escape(p.Name)};{Escape(p.CategoryName)};{p.Unit};{p.CurrentStock:N2};{p.MinimumStock:N2};{p.MaximumStock:N2};{p.CostPrice:N2};{p.SalePrice:N2};{p.ProfitMargin:N2}%;{statusText};{(p.IsActive ? "Sim" : "Não")}");
        }
        
        // Use UTF-8 with BOM for Excel compatibility
        var preamble = System.Text.Encoding.UTF8.GetPreamble();
        var content = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[preamble.Length + content.Length];
        preamble.CopyTo(result, 0);
        content.CopyTo(result, preamble.Length);
        
        return result;
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        // Escape quotes and wrap in quotes if contains separator or quotes
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
