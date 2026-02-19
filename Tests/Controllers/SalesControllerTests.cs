using System.Security.Claims;
using erp.Controllers;
using erp.DTOs.Sales;
using erp.Services.Authorization;
using erp.Services.Reports;
using erp.Services.Sales;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace erp.Tests.Controllers;

public class SalesControllerTests
{
    private readonly Mock<ISalesService> _salesService;
    private readonly Mock<ICustomerService> _customerService;
    private readonly Mock<IPdfExportService> _pdfExportService;
    private readonly Mock<IWebHostEnvironment> _webHostEnvironment;
    private readonly Mock<IPermissionService> _permissionService;
    private readonly Mock<ILogger<SalesController>> _logger;
    private readonly SalesController _controller;

    public SalesControllerTests()
    {
        _salesService = new Mock<ISalesService>();
        _customerService = new Mock<ICustomerService>();
        _pdfExportService = new Mock<IPdfExportService>();
        _webHostEnvironment = new Mock<IWebHostEnvironment>();
        _permissionService = new Mock<IPermissionService>();
        _logger = new Mock<ILogger<SalesController>>();

        _permissionService
            .Setup(x => x.HasModuleActionAccessAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _controller = new SalesController(
            _salesService.Object,
            _customerService.Object,
            _pdfExportService.Object,
            _webHostEnvironment.Object,
            _permissionService.Object,
            _logger.Object);

        SetUser("1");
    }

    [Fact]
    public async Task CreateCustomer_WhenPermissionDenied_ReturnsForbidden()
    {
        _permissionService
            .Setup(x => x.HasModuleActionAccessAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var result = await _controller.CreateCustomer(new CreateCustomerDto { Name = "Cliente", Document = "12345678901" });

        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task CreateCustomer_WithValidPayload_ReturnsCreated()
    {
        var expected = new CustomerDto { Id = 10, Name = "Cliente", Document = "12345678901" };
        _customerService.Setup(x => x.CreateAsync(It.IsAny<CreateCustomerDto>())).ReturnsAsync(expected);

        var result = await _controller.CreateCustomer(new CreateCustomerDto { Name = "Cliente", Document = "12345678901" });

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task CreateSale_WithoutUserIdClaim_ReturnsUnauthorized()
    {
        SetUser(null);

        var result = await _controller.CreateSale(new CreateSaleDto
        {
            Items = [new CreateSaleItemDto { ProductId = 1, Quantity = 1, UnitPrice = 10m }]
        });

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task GetCustomerById_WhenMissing_ReturnsNotFound()
    {
        _customerService.Setup(x => x.GetByIdAsync(999)).ReturnsAsync((CustomerDto?)null);

        var result = await _controller.GetCustomerById(999);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    private void SetUser(string? userId)
    {
        var claims = new List<Claim>();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        }

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"))
            }
        };
    }
}
