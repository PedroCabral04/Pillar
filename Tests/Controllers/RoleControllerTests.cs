using erp.Controllers;
using erp.DTOs.Role;
using erp.Models.Identity;
using Microsoft.AspNetCore.Identity;

namespace erp.Tests.Controllers;

/// <summary>
/// Testes unitários para o controlador de roles
/// </summary>
public class RoleControllerTests
{
    private readonly Mock<RoleManager<ApplicationRole>> _mockRoleManager;
    private readonly RoleController _controller;

    public RoleControllerTests()
    {
        var roleStore = new Mock<IRoleStore<ApplicationRole>>();
        _mockRoleManager = new Mock<RoleManager<ApplicationRole>>(
            roleStore.Object, null, null, null, null);

        _controller = new RoleController(_mockRoleManager.Object);
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
