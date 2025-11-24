using erp.Controllers;
using erp.DTOs.Role;
using erp.Models.Identity;
using erp.Services.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace erp.Tests.Controllers;

/// <summary>
/// Testes unitários para o controlador de roles
/// </summary>
public class RoleControllerTests
{
    private readonly Mock<RoleManager<ApplicationRole>> _mockRoleManager;
    private readonly Mock<ITenantContextAccessor> _mockTenantAccessor;
    private readonly TenantContext _tenantContext;
    private readonly RoleController _controller;

    public RoleControllerTests()
    {
        var roleStore = new Mock<IRoleStore<ApplicationRole>>();
        _mockRoleManager = new Mock<RoleManager<ApplicationRole>>(
            roleStore.Object, null, null, null, null);

        _mockTenantAccessor = new Mock<ITenantContextAccessor>();
        _tenantContext = new TenantContext();
        _mockTenantAccessor.SetupGet(x => x.Current).Returns(_tenantContext);

        _controller = new RoleController(_mockRoleManager.Object, _mockTenantAccessor.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(Array.Empty<Claim>(), "mock"))
                }
            }
        };
    }

    private void SetTenantClaim(int tenantId)
    {
        var claims = new List<Claim> { new(TenantClaimTypes.TenantId, tenantId.ToString()) };
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"))
        };
    }

    [Fact]
    public async Task GetAllRoles_ReturnsOkWithRoleList()
    {
        // Arrange
        var roles = new List<ApplicationRole>
        {
            new ApplicationRole { Id = 1, Name = "Admin", Abbreviation = "ADM" },
            new ApplicationRole { Id = 2, Name = "User", Abbreviation = "USR" },
            new ApplicationRole { Id = 3, Name = "Manager", Abbreviation = "MGR" }
        };

        _mockRoleManager.Setup(x => x.Roles)
            .Returns(roles.AsQueryable());

        // Act
        var result = await _controller.GetAllRoles();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedRoles = okResult.Value as List<RoleDto>;
        returnedRoles.Should().NotBeNull();
        returnedRoles.Should().HaveCount(3);
        returnedRoles![0].Name.Should().Be("Admin");
        returnedRoles[0].Abbreviation.Should().Be("ADM");
    }

    [Fact]
    public async Task GetAllRoles_WithTenantClaim_FiltersByTenant()
    {
        // Arrange
        var roles = new List<ApplicationRole>
        {
            new() { Id = 1, Name = "TenantAdmin", TenantId = 10 },
            new() { Id = 2, Name = "OtherTenantRole", TenantId = 20 }
        };

        _mockRoleManager.Setup(x => x.Roles)
            .Returns(roles.AsQueryable());

        SetTenantClaim(10);

        // Act
        var result = await _controller.GetAllRoles();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedRoles = okResult.Value as List<RoleDto>;
        returnedRoles.Should().NotBeNull();
        returnedRoles!.Should().HaveCount(1);
        returnedRoles[0].Id.Should().Be(1);
    }

    [Fact]
    public async Task GetAllRoles_WithNoRoles_ReturnsEmptyList()
    {
        // Arrange
        var roles = new List<ApplicationRole>();

        _mockRoleManager.Setup(x => x.Roles)
            .Returns(roles.AsQueryable());

        // Act
        var result = await _controller.GetAllRoles();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedRoles = okResult.Value as List<RoleDto>;
        returnedRoles.Should().NotBeNull();
        returnedRoles.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllRoles_WithRoleMissingAbbreviation_UsesNameAsAbbreviation()
    {
        // Arrange
        var roles = new List<ApplicationRole>
        {
            new ApplicationRole { Id = 1, Name = "Admin", Abbreviation = null }
        };

        _mockRoleManager.Setup(x => x.Roles)
            .Returns(roles.AsQueryable());

        // Act
        var result = await _controller.GetAllRoles();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedRoles = okResult.Value as List<RoleDto>;
        returnedRoles.Should().NotBeNull();
        returnedRoles.Should().HaveCount(1);
        returnedRoles![0].Abbreviation.Should().Be("Admin");
    }
}
