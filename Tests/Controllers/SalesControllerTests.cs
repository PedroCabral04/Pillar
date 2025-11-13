using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using erp.Controllers;
using erp.DTOs.Sales;
using erp.Services.Sales;

namespace erp.Tests.Controllers;

/// <summary>
/// Testes unitários para o controlador de vendas
/// Cobre operações de clientes e vendas
/// </summary>
public class SalesControllerTests
{
    private readonly Mock<ISalesService> _mockSalesService;
    private readonly Mock<ICustomerService> _mockCustomerService;
    private readonly Mock<ILogger<SalesController>> _mockLogger;
    private readonly SalesController _controller;

    public SalesControllerTests()
    {
        _mockSalesService = new Mock<ISalesService>();
        _mockCustomerService = new Mock<ICustomerService>();
        _mockLogger = new Mock<ILogger<SalesController>>();
        
        _controller = new SalesController(
            _mockSalesService.Object,
            _mockCustomerService.Object,
            _mockLogger.Object
        );

        // Setup user context
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "test@test.com"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    #region Customer Tests

    [Fact]
    public async Task CreateCustomer_WithValidData_ReturnsCreatedResult()
    {
        // Arrange
        var createDto = new CreateCustomerDto
        {
            Name = "Cliente Teste",
            Email = "cliente@teste.com",
            Phone = "11999999999",
            Document = "12345678901"
        };

        var expectedCustomer = new CustomerDto
        {
            Id = 1,
            Name = createDto.Name,
            Email = createDto.Email,
            Phone = createDto.Phone,
            Document = createDto.Document,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _mockCustomerService
            .Setup(s => s.CreateAsync(It.IsAny<CreateCustomerDto>()))
            .ReturnsAsync(expectedCustomer);

        // Act
        var result = await _controller.CreateCustomer(createDto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.Value.Should().BeEquivalentTo(expectedCustomer);
        _mockCustomerService.Verify(s => s.CreateAsync(It.IsAny<CreateCustomerDto>()), Times.Once);
    }

    [Fact]
    public async Task CreateCustomer_WithDuplicateDocument_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateCustomerDto
        {
            Name = "Cliente Teste",
            Email = "cliente@teste.com",
            Phone = "11999999999",
            Document = "12345678901"
        };

        _mockCustomerService
            .Setup(s => s.CreateAsync(It.IsAny<CreateCustomerDto>()))
            .ThrowsAsync(new InvalidOperationException("Documento já cadastrado"));

        // Act
        var result = await _controller.CreateCustomer(createDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateCustomer_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateCustomerDto
        {
            Name = "Cliente Teste",
            Email = "email-invalido",
            Document = "12345678901"
        };

        _controller.ModelState.AddModelError("Email", "E-mail inválido");

        // Act
        var result = await _controller.CreateCustomer(createDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateCustomer_WithCompleteAddress_ReturnsCreatedResult()
    {
        // Arrange
        var createDto = new CreateCustomerDto
        {
            Name = "Cliente Completo",
            Email = "cliente@teste.com",
            Phone = "11999999999",
            Mobile = "11988888888",
            Document = "12345678901",
            ZipCode = "01310100",
            Address = "Av. Paulista",
            Number = "1000",
            Complement = "Apto 101",
            Neighborhood = "Bela Vista",
            City = "São Paulo",
            State = "SP"
        };

        var expectedCustomer = new CustomerDto
        {
            Id = 1,
            Name = createDto.Name,
            Email = createDto.Email,
            Phone = createDto.Phone,
            Mobile = createDto.Mobile,
            Document = createDto.Document,
            ZipCode = createDto.ZipCode,
            Address = createDto.Address,
            Number = createDto.Number,
            Complement = createDto.Complement,
            Neighborhood = createDto.Neighborhood,
            City = createDto.City,
            State = createDto.State,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _mockCustomerService
            .Setup(s => s.CreateAsync(It.IsAny<CreateCustomerDto>()))
            .ReturnsAsync(expectedCustomer);

        // Act
        var result = await _controller.CreateCustomer(createDto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var customer = createdResult.Value as CustomerDto;
        customer.Should().NotBeNull();
        customer!.Address.Should().Be("Av. Paulista");
        customer.City.Should().Be("São Paulo");
        customer.State.Should().Be("SP");
    }

    [Fact]
    public async Task GetCustomerById_WithExistingId_ReturnsOk()
    {
        // Arrange
        var customerId = 1;
        var expectedCustomer = new CustomerDto
        {
            Id = customerId,
            Name = "Cliente Teste",
            Email = "cliente@teste.com",
            IsActive = true
        };

        _mockCustomerService
            .Setup(s => s.GetByIdAsync(customerId))
            .ReturnsAsync(expectedCustomer);

        // Act
        var result = await _controller.GetCustomerById(customerId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedCustomer);
    }

    [Fact]
    public async Task GetCustomerById_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var customerId = 999;
        _mockCustomerService
            .Setup(s => s.GetByIdAsync(customerId))
            .ReturnsAsync((CustomerDto?)null);

        // Act
        var result = await _controller.GetCustomerById(customerId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SearchCustomers_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        var customers = new List<CustomerDto>
        {
            new CustomerDto { Id = 1, Name = "Cliente 1", Email = "cliente1@teste.com", IsActive = true },
            new CustomerDto { Id = 2, Name = "Cliente 2", Email = "cliente2@teste.com", IsActive = true }
        };

        _mockCustomerService
            .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((customers, 2));

        // Act
        var result = await _controller.SearchCustomers("Cliente", true, 1, 10);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateCustomer_WithValidData_ReturnsOk()
    {
        // Arrange
        var customerId = 1;
        var updateDto = new UpdateCustomerDto
        {
            Name = "Cliente Atualizado",
            Email = "atualizado@teste.com",
            Phone = "11988888888"
        };

        var updatedCustomer = new CustomerDto
        {
            Id = customerId,
            Name = updateDto.Name,
            Email = updateDto.Email,
            Phone = updateDto.Phone,
            IsActive = true
        };

        _mockCustomerService
            .Setup(s => s.UpdateAsync(customerId, It.IsAny<UpdateCustomerDto>()))
            .ReturnsAsync(updatedCustomer);

        // Act
        var result = await _controller.UpdateCustomer(customerId, updateDto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(updatedCustomer);
    }

    [Fact]
    public async Task DeleteCustomer_WithExistingId_ReturnsOk()
    {
        // Arrange
        var customerId = 1;
        _mockCustomerService
            .Setup(s => s.DeleteAsync(customerId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteCustomer(customerId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region Sale Tests

    [Fact]
    public async Task CreateSale_WithValidData_ReturnsCreatedResult()
    {
        // Arrange
        var createDto = new CreateSaleDto
        {
            CustomerId = 1,
            SaleDate = DateTime.UtcNow,
            Items = new List<CreateSaleItemDto>
            {
                new CreateSaleItemDto { ProductId = 1, Quantity = 2, UnitPrice = 100 }
            },
            PaymentMethod = "Dinheiro",
            Status = "Pendente"
        };

        var expectedSale = new SaleDto
        {
            Id = 1,
            SaleNumber = "SALE-001",
            CustomerId = createDto.CustomerId,
            CustomerName = "Cliente Teste",
            UserId = 1,
            UserName = "Usuario Teste",
            TotalAmount = 200,
            NetAmount = 200,
            DiscountAmount = 0,
            Status = "Pendente",
            PaymentMethod = "Dinheiro",
            SaleDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            Items = new List<SaleItemDto>
            {
                new SaleItemDto 
                { 
                    Id = 1, 
                    ProductId = 1, 
                    ProductName = "Produto Teste",
                    ProductSku = "PROD-001",
                    Quantity = 2, 
                    UnitPrice = 100, 
                    Discount = 0,
                    Total = 200 
                }
            }
        };

        _mockSalesService
            .Setup(s => s.CreateAsync(It.IsAny<CreateSaleDto>(), It.IsAny<int>()))
            .ReturnsAsync(expectedSale);

        // Act
        var result = await _controller.CreateSale(createDto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var sale = createdResult.Value as SaleDto;
        sale.Should().NotBeNull();
        sale!.Id.Should().Be(1);
        sale.TotalAmount.Should().Be(200);
        sale.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateSale_WithMultipleItems_CalculatesTotalCorrectly()
    {
        // Arrange
        var createDto = new CreateSaleDto
        {
            CustomerId = 1,
            SaleDate = DateTime.UtcNow,
            Items = new List<CreateSaleItemDto>
            {
                new CreateSaleItemDto { ProductId = 1, Quantity = 2, UnitPrice = 100 },
                new CreateSaleItemDto { ProductId = 2, Quantity = 1, UnitPrice = 50 },
                new CreateSaleItemDto { ProductId = 3, Quantity = 3, UnitPrice = 30 }
            },
            PaymentMethod = "Cartão",
            Status = "Pendente"
        };

        var expectedSale = new SaleDto
        {
            Id = 1,
            SaleNumber = "SALE-001",
            CustomerId = 1,
            UserId = 1,
            TotalAmount = 340, // (2*100) + (1*50) + (3*30)
            NetAmount = 340,
            Status = "Pendente",
            Items = createDto.Items.Select((item, index) => new SaleItemDto
            {
                Id = index + 1,
                ProductId = item.ProductId,
                ProductName = $"Produto {item.ProductId}",
                ProductSku = $"PROD-00{item.ProductId}",
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Total = item.Quantity * item.UnitPrice
            }).ToList()
        };

        _mockSalesService
            .Setup(s => s.CreateAsync(It.IsAny<CreateSaleDto>(), It.IsAny<int>()))
            .ReturnsAsync(expectedSale);

        // Act
        var result = await _controller.CreateSale(createDto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var sale = createdResult.Value as SaleDto;
        sale.Should().NotBeNull();
        sale!.TotalAmount.Should().Be(340);
        sale.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task CreateSale_WithDiscount_AppliesDiscountCorrectly()
    {
        // Arrange
        var createDto = new CreateSaleDto
        {
            CustomerId = 1,
            SaleDate = DateTime.UtcNow,
            DiscountAmount = 50,
            Items = new List<CreateSaleItemDto>
            {
                new CreateSaleItemDto { ProductId = 1, Quantity = 2, UnitPrice = 100 }
            },
            PaymentMethod = "Dinheiro",
            Status = "Pendente"
        };

        var expectedSale = new SaleDto
        {
            Id = 1,
            SaleNumber = "SALE-001",
            TotalAmount = 200,
            DiscountAmount = 50,
            NetAmount = 150,
            Status = "Pendente"
        };

        _mockSalesService
            .Setup(s => s.CreateAsync(It.IsAny<CreateSaleDto>(), It.IsAny<int>()))
            .ReturnsAsync(expectedSale);

        // Act
        var result = await _controller.CreateSale(createDto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var sale = createdResult.Value as SaleDto;
        sale.Should().NotBeNull();
        sale!.DiscountAmount.Should().Be(50);
        sale.NetAmount.Should().Be(150);
    }

    [Fact]
    public async Task CreateSale_WithoutCustomer_ReturnsCreatedResult()
    {
        // Arrange - Venda sem cliente (venda avulsa)
        var createDto = new CreateSaleDto
        {
            CustomerId = null,
            SaleDate = DateTime.UtcNow,
            Items = new List<CreateSaleItemDto>
            {
                new CreateSaleItemDto { ProductId = 1, Quantity = 1, UnitPrice = 50 }
            },
            PaymentMethod = "Dinheiro",
            Status = "Pendente"
        };

        var expectedSale = new SaleDto
        {
            Id = 1,
            SaleNumber = "SALE-001",
            CustomerId = null,
            CustomerName = null,
            UserId = 1,
            TotalAmount = 50,
            Status = "Pendente"
        };

        _mockSalesService
            .Setup(s => s.CreateAsync(It.IsAny<CreateSaleDto>(), It.IsAny<int>()))
            .ReturnsAsync(expectedSale);

        // Act
        var result = await _controller.CreateSale(createDto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var sale = createdResult.Value as SaleDto;
        sale.Should().NotBeNull();
        sale!.CustomerId.Should().BeNull();
    }

    [Fact]
    public async Task CreateSale_WithInsufficientStock_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateSaleDto
        {
            CustomerId = 1,
            Items = new List<CreateSaleItemDto>
            {
                new CreateSaleItemDto { ProductId = 1, Quantity = 1000, UnitPrice = 100 }
            }
        };

        _mockSalesService
            .Setup(s => s.CreateAsync(It.IsAny<CreateSaleDto>(), It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("Estoque insuficiente"));

        // Act
        var result = await _controller.CreateSale(createDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetSaleById_WithExistingId_ReturnsOk()
    {
        // Arrange
        var saleId = 1;
        var expectedSale = new SaleDto
        {
            Id = saleId,
            SaleNumber = "SALE-001",
            Status = "Pendente",
            TotalAmount = 200
        };

        _mockSalesService
            .Setup(s => s.GetByIdAsync(saleId))
            .ReturnsAsync(expectedSale);

        // Act
        var result = await _controller.GetSaleById(saleId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedSale);
    }

    [Fact]
    public async Task SearchSales_ReturnsFilteredResults()
    {
        // Arrange
        var sales = new List<SaleDto>
        {
            new SaleDto { Id = 1, SaleNumber = "SALE-001", Status = "Pendente" },
            new SaleDto { Id = 2, SaleNumber = "SALE-002", Status = "Finalizada" }
        };

        _mockSalesService
            .Setup(s => s.SearchAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<int?>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync((sales, 2));

        // Act
        var result = await _controller.SearchSales(null, "Pendente", null, null, null, 1, 10);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task FinalizeSale_WithValidId_ReturnsOk()
    {
        // Arrange
        var saleId = 1;
        var finalizedSale = new SaleDto
        {
            Id = saleId,
            SaleNumber = "SALE-001",
            Status = "Finalizada",
            TotalAmount = 200
        };

        _mockSalesService
            .Setup(s => s.FinalizeAsync(saleId))
            .ReturnsAsync(finalizedSale);

        // Act
        var result = await _controller.FinalizeSale(saleId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(finalizedSale);
    }

    [Fact]
    public async Task CancelSale_WithValidId_ReturnsOk()
    {
        // Arrange
        var saleId = 1;
        _mockSalesService
            .Setup(s => s.CancelAsync(saleId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CancelSale(saleId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CancelSale_WithAlreadyCanceledSale_ReturnsBadRequest()
    {
        // Arrange
        var saleId = 1;
        _mockSalesService
            .Setup(s => s.CancelAsync(saleId))
            .ThrowsAsync(new InvalidOperationException("Venda já foi cancelada"));

        // Act
        var result = await _controller.CancelSale(saleId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CancelSale_WithFinalizedSale_ReturnsBadRequest()
    {
        // Arrange
        var saleId = 1;
        _mockSalesService
            .Setup(s => s.CancelAsync(saleId))
            .ThrowsAsync(new InvalidOperationException("Venda finalizada não pode ser cancelada"));

        // Act
        var result = await _controller.CancelSale(saleId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateSale_WithValidData_ReturnsOk()
    {
        // Arrange
        var saleId = 1;
        var updateDto = new UpdateSaleDto
        {
            CustomerId = 2,
            DiscountAmount = 20,
            Status = "Pendente",
            PaymentMethod = "Cartão",
            Notes = "Venda atualizada"
        };

        var updatedSale = new SaleDto
        {
            Id = saleId,
            SaleNumber = "SALE-001",
            CustomerId = updateDto.CustomerId,
            DiscountAmount = updateDto.DiscountAmount,
            Status = updateDto.Status,
            PaymentMethod = updateDto.PaymentMethod,
            Notes = updateDto.Notes,
            TotalAmount = 200,
            NetAmount = 180
        };

        _mockSalesService
            .Setup(s => s.UpdateAsync(saleId, It.IsAny<UpdateSaleDto>()))
            .ReturnsAsync(updatedSale);

        // Act
        var result = await _controller.UpdateSale(saleId, updateDto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var sale = okResult.Value as SaleDto;
        sale.Should().NotBeNull();
        sale!.DiscountAmount.Should().Be(20);
        sale.PaymentMethod.Should().Be("Cartão");
    }

    [Fact]
    public async Task SearchSales_WithDateRange_ReturnsFilteredResults()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;
        var sales = new List<SaleDto>
        {
            new SaleDto { Id = 1, SaleNumber = "SALE-001", SaleDate = DateTime.UtcNow.AddDays(-10) },
            new SaleDto { Id = 2, SaleNumber = "SALE-002", SaleDate = DateTime.UtcNow.AddDays(-5) }
        };

        _mockSalesService
            .Setup(s => s.SearchAsync(null, null, startDate, endDate, null, 1, 10))
            .ReturnsAsync((sales, 2));

        // Act
        var result = await _controller.SearchSales(null, null, startDate, endDate, null, 1, 10);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchSales_WithCustomerFilter_ReturnsCustomerSales()
    {
        // Arrange
        var customerId = 1;
        var sales = new List<SaleDto>
        {
            new SaleDto { Id = 1, SaleNumber = "SALE-001", CustomerId = customerId },
            new SaleDto { Id = 2, SaleNumber = "SALE-002", CustomerId = customerId }
        };

        _mockSalesService
            .Setup(s => s.SearchAsync(null, null, null, null, customerId, 1, 10))
            .ReturnsAsync((sales, 2));

        // Act
        var result = await _controller.SearchSales(null, null, null, null, customerId, 1, 10);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task FinalizeSale_WithNonExistingSale_ReturnsNotFound()
    {
        // Arrange
        var saleId = 999;
        _mockSalesService
            .Setup(s => s.FinalizeAsync(saleId))
            .ThrowsAsync(new KeyNotFoundException($"Venda com ID {saleId} não encontrada"));

        // Act
        var result = await _controller.FinalizeSale(saleId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateCustomer_WithNonExistingCustomer_ReturnsNotFound()
    {
        // Arrange
        var customerId = 999;
        var updateDto = new UpdateCustomerDto
        {
            Name = "Cliente Atualizado",
            Email = "novo@teste.com"
        };

        _mockCustomerService
            .Setup(s => s.UpdateAsync(customerId, It.IsAny<UpdateCustomerDto>()))
            .ThrowsAsync(new KeyNotFoundException($"Cliente com ID {customerId} não encontrado"));

        // Act
        var result = await _controller.UpdateCustomer(customerId, updateDto);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteCustomer_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var customerId = 999;
        _mockCustomerService
            .Setup(s => s.DeleteAsync(customerId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteCustomer(customerId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion
}

