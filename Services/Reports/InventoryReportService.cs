using erp.Data;
using erp.DTOs.Reports;
using Microsoft.EntityFrameworkCore;

namespace erp.Services.Reports;

public class InventoryReportService : IInventoryReportService
{
    // ABC Curve (Pareto) analysis thresholds
    private const decimal ClassAThreshold = 80m;  // First 80% of revenue
    private const decimal ClassBThreshold = 95m;  // 80% to 95% of revenue
    // Class C: remaining 5% (95% to 100%)

    private readonly ApplicationDbContext _context;
    private readonly ILogger<InventoryReportService> _logger;

    public InventoryReportService(ApplicationDbContext context, ILogger<InventoryReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<StockLevelsReportDto> GenerateStockLevelsReportAsync(InventoryReportFilterDto filter)
    {
        try
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .AsQueryable();

            if (filter.ProductId.HasValue)
            {
                query = query.Where(p => p.Id == filter.ProductId.Value);
            }

            if (filter.CategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
            }

            if (filter.OnlyLowStock == true)
            {
                query = query.Where(p => p.CurrentStock <= p.MinimumStock);
            }

            var products = await query.ToListAsync();

            var items = products.Select(p =>
            {
                var status = "OK";
                if (p.CurrentStock == 0)
                    status = "Sem Estoque";
                else if (p.CurrentStock <= p.MinimumStock)
                    status = "Estoque Baixo";
                else if (p.CurrentStock <= (p.MinimumStock * 1.2m))
                    status = "Atenção";

                return new StockLevelItemDto
                {
                    ProductId = p.Id,
                    Sku = p.Sku,
                    ProductName = p.Name,
                    Category = p.Category?.Name ?? "Sem categoria",
                    CurrentStock = p.CurrentStock,
                    MinimumStock = p.MinimumStock,
                    MaximumStock = p.MaximumStock,
                    Unit = p.Unit,
                    CostPrice = p.CostPrice,
                    SalePrice = p.SalePrice,
                    TotalValue = p.CurrentStock * p.CostPrice,
                    Status = status
                };
            }).ToList();

            var summary = new StockLevelsSummaryDto
            {
                TotalProducts = items.Count,
                ProductsInStock = items.Count(i => i.CurrentStock > i.MinimumStock),
                ProductsLowStock = items.Count(i => i.CurrentStock > 0 && i.CurrentStock <= i.MinimumStock),
                ProductsOutOfStock = items.Count(i => i.CurrentStock == 0),
                TotalInventoryValue = items.Sum(i => i.TotalValue)
            };

            return new StockLevelsReportDto
            {
                Items = items,
                Summary = summary
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório de níveis de estoque");
            throw;
        }
    }

    public async Task<StockMovementReportDto> GenerateStockMovementReportAsync(InventoryReportFilterDto filter)
    {
        try
        {
            var query = _context.StockMovements
                .Include(m => m.Product)
                .Include(m => m.CreatedByUser)
                .AsQueryable();

            if (filter.StartDate.HasValue)
            {
                var startDate = filter.StartDate.Value.ToUniversalTime();
                query = query.Where(m => m.MovementDate >= startDate);
            }

            if (filter.EndDate.HasValue)
            {
                var endDate = filter.EndDate.Value.ToUniversalTime();
                query = query.Where(m => m.MovementDate <= endDate);
            }

            if (filter.ProductId.HasValue)
            {
                query = query.Where(m => m.ProductId == filter.ProductId.Value);
            }

            if (!string.IsNullOrEmpty(filter.MovementType))
            {
                query = query.Where(m => m.Type.ToString() == filter.MovementType);
            }

            var movements = await query.OrderByDescending(m => m.MovementDate).ToListAsync();

            var items = movements.Select(m => new StockMovementItemDto
            {
                MovementId = m.Id,
                Date = m.MovementDate,
                ProductName = m.Product?.Name ?? "N/A",
                Sku = m.Product?.Sku ?? "N/A",
                Type = m.Type.ToString(),
                Reason = m.Reason.ToString(),
                Quantity = m.Quantity,
                Unit = m.Product?.Unit ?? "UN",
                UnitCost = m.UnitCost,
                TotalCost = m.Quantity * m.UnitCost,
                ReferenceDocument = m.DocumentNumber,
                UserName = m.CreatedByUser?.FullName ?? m.CreatedByUser?.UserName ?? "N/A"
            }).ToList();

            var summary = new StockMovementSummaryDto
            {
                TotalMovements = items.Count,
                MovementsByType = items.GroupBy(i => i.Type)
                    .ToDictionary(g => g.Key, g => g.Count()),
                TotalValueMovements = items.Sum(i => Math.Abs(i.TotalCost))
            };

            return new StockMovementReportDto
            {
                Items = items,
                Summary = summary
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório de movimentação de estoque");
            throw;
        }
    }

    public async Task<InventoryValuationReportDto> GenerateInventoryValuationReportAsync(InventoryReportFilterDto filter)
    {
        try
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.CurrentStock > 0)
                .AsQueryable();

            if (filter.CategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
            }

            var products = await query.ToListAsync();

            var items = products
                .GroupBy(p => p.Category?.Name ?? "Sem categoria")
                .Select(g =>
                {
                    var totalCostValue = g.Sum(p => p.CurrentStock * p.CostPrice);
                    var totalSaleValue = g.Sum(p => p.CurrentStock * p.SalePrice);
                    var potentialProfit = totalSaleValue - totalCostValue;

                    return new InventoryValuationItemDto
                    {
                        Category = g.Key,
                        ProductCount = g.Count(),
                        TotalQuantity = g.Sum(p => p.CurrentStock),
                        TotalCostValue = totalCostValue,
                        TotalSaleValue = totalSaleValue,
                        PotentialProfit = potentialProfit,
                        ProfitMargin = totalCostValue > 0 ? (potentialProfit / totalCostValue * 100) : 0
                    };
                })
                .OrderByDescending(i => i.TotalCostValue)
                .ToList();

            var summary = new InventoryValuationSummaryDto
            {
                TotalCostValue = items.Sum(i => i.TotalCostValue),
                TotalSaleValue = items.Sum(i => i.TotalSaleValue),
                TotalPotentialProfit = items.Sum(i => i.PotentialProfit),
                AverageProfitMargin = items.Any() ? items.Average(i => i.ProfitMargin) : 0
            };

            return new InventoryValuationReportDto
            {
                Items = items,
                Summary = summary
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório de avaliação de estoque");
            throw;
        }
    }

    public async Task<ABCCurveReportDto> GenerateABCCurveReportAsync(InventoryReportFilterDto filter)
    {
        try
        {
            // Get sales data grouped by product
            var salesQuery = _context.SaleItems
                .Include(si => si.Product)
                    .ThenInclude(p => p.Category)
                .Include(si => si.Sale)
                .Where(si => si.Sale.Status != "Cancelada")
                .AsQueryable();

            // Apply date filters
            if (filter.StartDate.HasValue)
            {
                var startDate = filter.StartDate.Value.ToUniversalTime();
                salesQuery = salesQuery.Where(si => si.Sale.SaleDate >= startDate);
            }

            if (filter.EndDate.HasValue)
            {
                var endDate = filter.EndDate.Value.ToUniversalTime();
                salesQuery = salesQuery.Where(si => si.Sale.SaleDate <= endDate);
            }

            if (filter.CategoryId.HasValue)
            {
                salesQuery = salesQuery.Where(si => si.Product.CategoryId == filter.CategoryId.Value);
            }

            // Group by product and calculate revenue
            var productSales = await salesQuery
                .GroupBy(si => new 
                { 
                    si.ProductId, 
                    si.Product.Sku, 
                    si.Product.Name, 
                    CategoryName = si.Product.Category != null ? si.Product.Category.Name : "Sem categoria" 
                })
                .Select(g => new
                {
                    g.Key.ProductId,
                    g.Key.Sku,
                    g.Key.Name,
                    g.Key.CategoryName,
                    QuantitySold = (int)g.Sum(si => si.Quantity),
                    TotalRevenue = g.Sum(si => si.Total)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .ToListAsync();

            var totalRevenue = productSales.Sum(x => x.TotalRevenue);

            // Calculate percentages and classifications
            var items = new List<ABCCurveItemDto>();
            decimal cumulativePercentage = 0;

            foreach (var product in productSales)
            {
                var revenuePercentage = totalRevenue > 0 ? (product.TotalRevenue / totalRevenue) * 100 : 0;
                cumulativePercentage += revenuePercentage;

                // Classify based on cumulative percentage thresholds
                string classification;
                if (cumulativePercentage <= ClassAThreshold)
                    classification = "A";
                else if (cumulativePercentage <= ClassBThreshold)
                    classification = "B";
                else
                    classification = "C";

                items.Add(new ABCCurveItemDto
                {
                    ProductId = product.ProductId,
                    Sku = product.Sku,
                    ProductName = product.Name,
                    Category = product.CategoryName,
                    QuantitySold = product.QuantitySold,
                    TotalRevenue = product.TotalRevenue,
                    RevenuePercentage = revenuePercentage,
                    CumulativePercentage = cumulativePercentage,
                    Classification = classification
                });
            }

            // Calculate summary
            var classAItems = items.Where(i => i.Classification == "A").ToList();
            var classBItems = items.Where(i => i.Classification == "B").ToList();
            var classCItems = items.Where(i => i.Classification == "C").ToList();

            var summary = new ABCCurveSummaryDto
            {
                TotalProducts = items.Count,
                TotalRevenue = totalRevenue,
                ClassACount = classAItems.Count,
                ClassARevenue = classAItems.Sum(i => i.TotalRevenue),
                ClassAPercentage = totalRevenue > 0 ? (classAItems.Sum(i => i.TotalRevenue) / totalRevenue) * 100 : 0,
                ClassBCount = classBItems.Count,
                ClassBRevenue = classBItems.Sum(i => i.TotalRevenue),
                ClassBPercentage = totalRevenue > 0 ? (classBItems.Sum(i => i.TotalRevenue) / totalRevenue) * 100 : 0,
                ClassCCount = classCItems.Count,
                ClassCRevenue = classCItems.Sum(i => i.TotalRevenue),
                ClassCPercentage = totalRevenue > 0 ? (classCItems.Sum(i => i.TotalRevenue) / totalRevenue) * 100 : 0
            };

            return new ABCCurveReportDto
            {
                Items = items,
                Summary = summary
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório de curva ABC");
            throw;
        }
    }
}
