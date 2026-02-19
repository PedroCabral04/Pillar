using System.Security.Claims;
using erp.Controllers;
using erp.Data;
using erp.DTOs.Role;
using erp.Models.Identity;
using erp.Services.Tenancy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace erp.Tests.Controllers;

public class RoleControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<RoleManager<ApplicationRole>> _roleManager;
    private readonly Mock<ITenantContextAccessor> _tenantAccessor;

    public RoleControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _tenantAccessor = new Mock<ITenantContextAccessor>();
        _tenantAccessor.SetupGet(x => x.Current).Returns(new TenantContext());

        _context = new ApplicationDbContext(options, tenantContextAccessor: _tenantAccessor.Object);

        var roleStore = new Mock<IRoleStore<ApplicationRole>>();
        _roleManager = new Mock<RoleManager<ApplicationRole>>(
            roleStore.Object,
            null!,
            null!,
            null!,
            null!);
    }

    [Fact]
    public async Task GetAllRoles_ReturnsOkWithRoleList()
    {
        _context.Set<ApplicationRole>().AddRange(
            new ApplicationRole { Id = 1, Name = "Admin", Abbreviation = "ADM", NormalizedName = "ADMIN" },
            new ApplicationRole { Id = 2, Name = "User", Abbreviation = "USR", NormalizedName = "USER" });
        await _context.SaveChangesAsync();
        _roleManager.SetupGet(r => r.Roles).Returns(_context.Set<ApplicationRole>());

        var controller = CreateController();
        var result = await controller.GetAllRoles();

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedRoles = okResult.Value.Should().BeAssignableTo<List<RoleDto>>().Subject;
        returnedRoles.Should().HaveCount(2);
        returnedRoles.Should().Contain(r => r.Name == "Admin" && r.Abbreviation == "ADM");
    }

    [Fact]
    public async Task GetAllRoles_WithTenantClaim_FiltersByTenant()
    {
        _context.Set<ApplicationRole>().AddRange(
            new ApplicationRole { Id = 1, Name = "TenantAdmin", TenantId = 10, NormalizedName = "TENANTADMIN" },
            new ApplicationRole { Id = 2, Name = "GlobalRole", TenantId = null, NormalizedName = "GLOBALROLE" },
            new ApplicationRole { Id = 3, Name = "OtherTenantRole", TenantId = 20, NormalizedName = "OTHERTENANTROLE" });
        await _context.SaveChangesAsync();
        _roleManager.SetupGet(r => r.Roles).Returns(_context.Set<ApplicationRole>());

        var controller = CreateController(new Claim(TenantClaimTypes.TenantId, "10"));
        var result = await controller.GetAllRoles();

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedRoles = okResult.Value.Should().BeAssignableTo<List<RoleDto>>().Subject;
        returnedRoles.Should().HaveCount(2);
        returnedRoles.Select(r => r.Id).Should().BeEquivalentTo([1, 2]);
    }

    [Fact]
    public async Task GetAllRoles_WithRoleMissingAbbreviation_UsesNameAsAbbreviation()
    {
        _context.Set<ApplicationRole>().Add(new ApplicationRole { Id = 1, Name = "Admin", Abbreviation = null, NormalizedName = "ADMIN" });
        await _context.SaveChangesAsync();
        _roleManager.SetupGet(r => r.Roles).Returns(_context.Set<ApplicationRole>());

        var controller = CreateController();
        var result = await controller.GetAllRoles();

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedRoles = okResult.Value.Should().BeAssignableTo<List<RoleDto>>().Subject;
        returnedRoles.Should().HaveCount(1);
        returnedRoles[0].Abbreviation.Should().Be("Admin");
    }

    private RoleController CreateController(params Claim[] claims)
    {
        return new RoleController(_roleManager.Object, _tenantAccessor.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"))
                }
            }
        };
    }

    public void Dispose()
    {
        _roleManager.Object.Dispose();
        _context.Dispose();
    }
}
