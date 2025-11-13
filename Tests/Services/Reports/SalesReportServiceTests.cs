using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using erp.Data;
using erp.DTOs.Reports;
using erp.Models.Identity;
using erp.Models.Inventory;
using erp.Models.Sales;
using erp.Services.Reports;

namespace erp.Tests.Services.Reports;

/// <summary>
/// Testes unitários para o serviço de relatórios de vendas
/// </summary>
public class SalesReportServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<SalesReportService>> _mockLogger;
    private readonly SalesReportService _service;

    public SalesReportServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<SalesReportService>>();
        _service = new SalesReportService(_context, _mockLogger.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var customer1 = new Customer
        {
            Id = 1,
            Name = "Cliente Teste 1",
            Email = "cliente1@teste.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var customer2 = new Customer
        {
            Id = 2,
            Name = "Cliente Teste 2",
            Email = "cliente2@teste.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var user = new ApplicationUser
        {
            Id = 1,
            UserName = "vendedor@teste.com",
            Email = "vendedor@teste.com",
            FullName = "Vendedor Teste"
        };

        var product1 = new Product
        {
            Id = 1,
            Name = "Produto 1",
            Sku = "PROD-001",
            SalePrice = 100,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var product2 = new Product
        {
            Id = 2,
            Name = "Produto 2",
            Sku = "PROD-002",
            SalePrice = 50,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var sale1 = new Sale
        {
            Id = 1,
            SaleNumber = "SALE-001",
            CustomerId = 1,
            UserId = 1,
            SaleDate = DateTime.UtcNow.AddDays(-10),
            TotalAmount = 200,
            NetAmount = 180,
            DiscountAmount = 20,
            Status = "Finalizada",
            PaymentMethod = "Dinheiro",
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            Items = new List<SaleItem>
            {
                new SaleItem
                {
                    Id = 1,
                    ProductId = 1,
                    Quantity = 2,
                    UnitPrice = 100,
                    Discount = 10,
                    Total = 190
                }
            }
        };

        var sale2 = new Sale
        {
            Id = 2,
            SaleNumber = "SALE-002",
            CustomerId = 2,
            UserId = 1,
            SaleDate = DateTime.UtcNow.AddDays(-5),
            TotalAmount = 150,
            NetAmount = 150,
            Status = "Finalizada",
            PaymentMethod = "Cartão",
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            Items = new List<SaleItem>
            {
                new SaleItem
                {
                    Id = 2,
                    ProductId = 2,
                    Quantity = 3,
                    UnitPrice = 50,
                    Total = 150
                }
            }
        };

        var sale3 = new Sale
        {
            Id = 3,
            SaleNumber = "SALE-003",
            CustomerId = 1,
            UserId = 1,
            SaleDate = DateTime.UtcNow.AddDays(-2),
            TotalAmount = 250,
            NetAmount = 250,
            Status = "Pendente",
            PaymentMethod = "PIX",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            Items = new List<SaleItem>
            {
                new SaleItem
                {
                    Id = 3,
                    ProductId = 1,
                    Quantity = 1,
                    UnitPrice = 100,
                    Total = 100
                },
                new SaleItem
                {
                    Id = 4,
                    ProductId = 2,
                    Quantity = 3,
                    UnitPrice = 50,
                    Total = 150
                }
            }
        };

        _context.Customers.AddRange(customer1, customer2);
    // Add ApplicationUser to Identity set (not legacy Users DbSet)
    _context.Set<ApplicationUser>().Add(user);
        _context.Products.AddRange(product1, product2);
        _context.Sales.AddRange(sale1, sale2, sale3);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GenerateSalesReport_WithNoFilters_ReturnsAllSales()
    {
        // Arrange
        var filter = new SalesReportFilterDto
        {
            StartDate = null,
            EndDate = null
        };

        // Act
        var result = await _service.GenerateSalesReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalSales.Should().Be(3);
        result.TotalAmount.Should().Be(600); // 200 + 150 + 250
        result.Sales.Should().HaveCount(3);
    }

    [Fact]
    public async Task GenerateSalesReport_WithDateRange_ReturnsFilteredSales()
    {
        // Arrange
        var filter = new SalesReportFilterDto
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow
        };

        // Act
        var result = await _service.GenerateSalesReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalSales.Should().Be(2); // Only sales from last 7 days
        result.Sales.Should().HaveCount(2);
    }

    [Fact]
    public async Task GenerateSalesReport_WithCustomerFilter_ReturnsCustomerSales()
    {
        // Arrange
        var filter = new SalesReportFilterDto
        {
            CustomerId = 1
        };

        // Act
        var result = await _service.GenerateSalesReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalSales.Should().Be(2); // Customer 1 has 2 sales
        result.Sales.Should().OnlyContain(s => s.CustomerId == 1);
    }

    [Fact]
    public async Task GenerateSalesReport_WithStatusFilter_ReturnsFilteredSales()
    {
        // Arrange
        var filter = new SalesReportFilterDto
        {
            Status = "Finalizada"
        };

        // Act
        var result = await _service.GenerateSalesReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalSales.Should().Be(2); // 2 finalized sales
        result.Sales.Should().OnlyContain(s => s.Status == "Finalizada");
    }

    [Fact]
    public async Task GenerateSalesReport_WithPaymentMethodFilter_ReturnsFilteredSales()
    {
        // Arrange
        var filter = new SalesReportFilterDto
        {
            PaymentMethod = "Dinheiro"
        };

        // Act
        var result = await _service.GenerateSalesReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalSales.Should().Be(1);
        result.Sales.Should().OnlyContain(s => s.PaymentMethod == "Dinheiro");
    }

    [Fact]
    public async Task GenerateSalesReport_WithSalespersonFilter_ReturnsSalespersonSales()
    {
        // Arrange
        var filter = new SalesReportFilterDto
        {
            SalespersonId = 1
        };

        // Act
        var result = await _service.GenerateSalesReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalSales.Should().Be(3); // All sales by user 1
        result.Sales.Should().OnlyContain(s => s.UserId == 1);
    }

    [Fact]
    public async Task GenerateSalesReport_CalculatesTotalsCorrectly()
    {
        // Arrange
        var filter = new SalesReportFilterDto();

        // Act
        var result = await _service.GenerateSalesReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalAmount.Should().Be(600);
        result.AverageTicket.Should().Be(200); // 600 / 3
        result.TotalDiscount.Should().Be(20); // Only sale1 has discount
    }

    [Fact]
    public async Task GenerateSalesReport_IncludesCustomerNames()
    {
        // Arrange
        var filter = new SalesReportFilterDto();

        // Act
        var result = await _service.GenerateSalesReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
    result.Items.Should().Contain(s => s.CustomerName == "Cliente Teste 1");
    result.Items.Should().Contain(s => s.CustomerName == "Cliente Teste 2");
    }

    [Fact]
    public async Task GenerateSalesReport_IncludesUserNames()
    {
        // Arrange
        var filter = new SalesReportFilterDto();

        // Act
        var result = await _service.GenerateSalesReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
    result.Items.Should().OnlyContain(s => s.SalespersonName == "Vendedor Teste");
    }

    [Fact]
    public async Task GenerateSalesReport_WithMultipleFilters_AppliesAllFilters()
    {
        // Arrange
        var filter = new SalesReportFilterDto
        {
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            CustomerId = 1,
            Status = "Pendente"
        };

        // Act
        var result = await _service.GenerateSalesReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalSales.Should().Be(1); // Only one sale matches all filters
        result.Sales.Should().OnlyContain(s => 
            s.CustomerId == 1 && 
            s.Status == "Pendente" &&
            s.SaleDate >= filter.StartDate &&
            s.SaleDate <= filter.EndDate);
    }

    [Fact]
    public async Task GenerateSalesReport_WithNoMatchingData_ReturnsEmptyResult()
    {
        // Arrange
        var filter = new SalesReportFilterDto
        {
            CustomerId = 999 // Non-existing customer
        };

        // Act
        var result = await _service.GenerateSalesReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalSales.Should().Be(0);
        result.TotalAmount.Should().Be(0);
        result.Sales.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateSalesReport_IncludesSaleItems()
    {
        // Arrange
        var filter = new SalesReportFilterDto();

        // Act
        var result = await _service.GenerateSalesReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        var saleWithMultipleItems = result.Sales.FirstOrDefault(s => s.Id == 3);
        saleWithMultipleItems.Should().NotBeNull();
        saleWithMultipleItems!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GenerateSalesReport_OrdersByDateDescending()
    {
        // Arrange
        var filter = new SalesReportFilterDto();

        // Act
        var result = await _service.GenerateSalesReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.Sales.Should().HaveCount(3);
        var dates = result.Sales.Select(s => s.SaleDate).ToList();
        dates.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GenerateByCustomerReport_GroupsByCustomer()
    {
        // Arrange
        var filter = new SalesReportFilterDto();

        // Act
        var result = await _service.GenerateByCustomerReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalCustomers.Should().Be(2);
        result.Customers.Should().HaveCount(2);
    }

    [Fact]
    public async Task GenerateByCustomerReport_CalculatesCustomerTotals()
    {
        // Arrange
        var filter = new SalesReportFilterDto();

        // Act
        var result = await _service.GenerateByCustomerReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        var customer1Data = result.Customers.FirstOrDefault(c => c.CustomerId == 1);
        customer1Data.Should().NotBeNull();
        customer1Data!.TotalSales.Should().Be(2); // Customer 1 has 2 sales
        customer1Data.TotalAmount.Should().Be(450); // 200 + 250
    }

    [Fact]
    public async Task GenerateByProductReport_GroupsByProduct()
    {
        // Arrange
        var filter = new SalesReportFilterDto();

        // Act
        var result = await _service.GenerateByProductReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalProducts.Should().Be(2);
        result.Products.Should().HaveCount(2);
    }

    [Fact]
    public async Task GenerateByProductReport_CalculatesProductTotals()
    {
        // Arrange
        var filter = new SalesReportFilterDto();

        // Act
        var result = await _service.GenerateByProductReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        var product1Data = result.Products.FirstOrDefault(p => p.ProductId == 1);
        product1Data.Should().NotBeNull();
        product1Data!.QuantitySold.Should().Be(3); // 2 + 1 from different sales
        product1Data.TotalRevenue.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateByPaymentMethodReport_GroupsByPaymentMethod()
    {
        // Arrange
        var filter = new SalesReportFilterDto();

        // Act
        var result = await _service.GenerateByPaymentMethodReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalMethods.Should().Be(3); // Dinheiro, Cartão, PIX
        result.PaymentMethods.Should().HaveCount(3);
    }

    [Fact]
    public async Task GenerateByPaymentMethodReport_CalculatesMethodTotals()
    {
        // Arrange
        var filter = new SalesReportFilterDto();

        // Act
        var result = await _service.GenerateByPaymentMethodReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        var cashData = result.PaymentMethods.FirstOrDefault(pm => pm.PaymentMethod == "Dinheiro");
        cashData.Should().NotBeNull();
        cashData!.TotalSales.Should().Be(1);
        cashData.TotalAmount.Should().Be(200);
    }

    [Fact]
    public async Task GenerateSalesReport_HandlesNullCustomer()
    {
        // Arrange - Add a sale without customer
        var saleWithoutCustomer = new Sale
        {
            Id = 4,
            SaleNumber = "SALE-004",
            CustomerId = null,
            UserId = 1,
            SaleDate = DateTime.UtcNow,
            TotalAmount = 100,
            NetAmount = 100,
            Status = "Finalizada",
            PaymentMethod = "Dinheiro",
            CreatedAt = DateTime.UtcNow,
            Items = new List<SaleItem>()
        };

        _context.Sales.Add(saleWithoutCustomer);
        await _context.SaveChangesAsync();

        var filter = new SalesReportFilterDto();

        // Act
        var result = await _service.GenerateSalesReportAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.TotalSales.Should().Be(4);
        result.Sales.Should().Contain(s => s.CustomerId == null);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
