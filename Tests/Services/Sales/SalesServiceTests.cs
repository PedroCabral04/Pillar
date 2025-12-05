using erp.Data;
using erp.DTOs.Sales;
using erp.Models.Inventory;
using erp.Models.Sales;
using erp.Services.Sales;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace erp.Tests.Services.Sales;

public class SalesServiceTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public SalesServiceTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateAsync_ShouldSaveDatesInUtc()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<ISalesService>();

        // Setup dependencies (Customer, Product)
        var customer = new Customer { Name = "Test Customer", Document = "12345678901", TenantId = 1 };
        
        // Ensure category exists
        var category = new ProductCategory { Name = "Test Cat", Code = "TEST" };
        context.ProductCategories.Add(category);
        await context.SaveChangesAsync();
        
        var product = new Product { Name = "Test Product", Sku = "TEST-SKU", SalePrice = 100, CurrentStock = 10, TenantId = 1, CategoryId = category.Id };
        
        context.Customers.Add(customer);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var dto = new CreateSaleDto
        {
            CustomerId = customer.Id,
            SaleDate = DateTime.Now, // Passing local time
            Items = new List<CreateSaleItemDto>
            {
                new() { ProductId = product.Id, Quantity = 1, UnitPrice = 100 }
            }
        };

        // Act
        var result = await service.CreateAsync(dto, 1);

        // Assert
        var savedSale = await context.Sales.FindAsync(result.Id);
        savedSale.Should().NotBeNull();
        
        // The SaleDate should be converted to UTC in the DB
        savedSale!.SaleDate.Kind.Should().Be(DateTimeKind.Utc);
        
        // Also check CreatedAt
        savedSale.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task SearchAsync_ShouldBeCaseInsensitiveForStatus()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<ISalesService>();

        var sale = new Sale
        {
            SaleNumber = "VEN-TEST-001",
            Status = "Finalizada", // Stored as mixed/title case
            SaleDate = DateTime.UtcNow,
            UserId = 1
        };
        context.Sales.Add(sale);
        await context.SaveChangesAsync();

        // Act
        // Search with lowercase "finalizada"
        var (items, total) = await service.SearchAsync(null, "finalizada", null, null, null, 1, 10);

        // Assert
        items.Should().Contain(s => s.Id == sale.Id);
        total.Should().BeGreaterThanOrEqualTo(1);
        
        // Search with uppercase "FINALIZADA"
        var (itemsUpper, totalUpper) = await service.SearchAsync(null, "FINALIZADA", null, null, null, 1, 10);
        itemsUpper.Should().Contain(s => s.Id == sale.Id);
    }
    
    [Fact]
    public async Task FinalizeAsync_ShouldHandleCaseInsensitiveStatusCheck()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<ISalesService>();

        var category = new ProductCategory { Name = "Test Cat 2", Code = "TEST2" };
        context.ProductCategories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product { Name = "Test Product 2", Sku = "TEST-SKU-2", SalePrice = 100, CurrentStock = 10, TenantId = 1, CategoryId = category.Id };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var sale = new Sale
        {
            SaleNumber = "VEN-TEST-002",
            Status = "Pendente",
            SaleDate = DateTime.UtcNow,
            UserId = 1
        };
        sale.Items.Add(new SaleItem { ProductId = product.Id, Quantity = 1, UnitPrice = 100, Total = 100 });
        context.Sales.Add(sale);
        await context.SaveChangesAsync();

        // Act
        var result = await service.FinalizeAsync(sale.Id);

        // Assert
        result.Status.Should().Be("Finalizada");
        
        // Verify stock deduction
        var updatedProduct = await context.Products.FindAsync(product.Id);
        updatedProduct!.CurrentStock.Should().Be(9);
    }
}
