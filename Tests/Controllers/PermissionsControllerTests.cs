using System.Security.Claims;
using erp.Controllers;
using erp.Data;
using erp.DTOs.Permissions;
using erp.Models.Identity;
using erp.Services.Authorization;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace erp.Tests.Controllers;

public class PermissionsControllerTests
{
    [Fact]
    public async Task GetAllModules_ShouldIncludeActionDefinitions()
    {
        await using var context = CreateContext();
        context.ModulePermissions.Add(new ModulePermission
        {
            Id = 1,
            ModuleKey = ModuleKeys.Sales,
            DisplayName = "Vendas",
            DisplayOrder = 1,
            IsActive = true
        });

        context.ModuleActionPermissions.Add(new ModuleActionPermission
        {
            Id = 10,
            ModulePermissionId = 1,
            ActionKey = "view_page",
            DisplayName = "Visualizar página",
            DisplayOrder = 1,
            IsActive = true
        });

        await context.SaveChangesAsync();

        var controller = CreateController(context, Mock.Of<IPermissionService>());

        var result = await controller.GetAllModules();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var modules = ok.Value.Should().BeAssignableTo<List<ModulePermissionDto>>().Subject;
        modules.Should().ContainSingle();
        modules[0].Actions.Should().ContainSingle(a => a.ActionKey == "view_page");
    }

    [Fact]
    public async Task GetAllRolesWithPermissions_ShouldIncludeGrantedActionIds()
    {
        await using var context = CreateContext();

        var role = new ApplicationRole { Id = 7, Name = "Vendedor", NormalizedName = "VENDEDOR" };
        var module = new ModulePermission
        {
            Id = 2,
            ModuleKey = ModuleKeys.Sales,
            DisplayName = "Vendas",
            DisplayOrder = 1,
            IsActive = true
        };

        var action = new ModuleActionPermission
        {
            Id = 20,
            ModulePermissionId = module.Id,
            ActionKey = "view_values",
            DisplayName = "Ver valores",
            DisplayOrder = 1,
            IsActive = true
        };

        context.Set<ApplicationRole>().Add(role);
        context.ModulePermissions.Add(module);
        context.ModuleActionPermissions.Add(action);
        context.RoleModulePermissions.Add(new RoleModulePermission { RoleId = role.Id, ModulePermissionId = module.Id });
        context.RoleModuleActionPermissions.Add(new RoleModuleActionPermission
        {
            RoleId = role.Id,
            ModuleActionPermissionId = action.Id
        });

        await context.SaveChangesAsync();

        var controller = CreateController(context, Mock.Of<IPermissionService>());

        var result = await controller.GetAllRolesWithPermissions();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var roles = ok.Value.Should().BeAssignableTo<List<RolePermissionsDto>>().Subject;
        roles.Should().ContainSingle();
        roles[0].GrantedModuleActionIds.Should().Contain(action.Id);
    }

    [Fact]
    public async Task UpdateRoleModules_ShouldPersistModulesAndActions()
    {
        await using var context = CreateContext();
        var role = new ApplicationRole { Id = 3, Name = "Gerente", NormalizedName = "GERENTE" };
        context.ModulePermissions.AddRange(
            new ModulePermission { Id = 1, ModuleKey = ModuleKeys.Sales, DisplayName = "Vendas", DisplayOrder = 1, IsActive = true },
            new ModulePermission { Id = 2, ModuleKey = ModuleKeys.Inventory, DisplayName = "Estoque", DisplayOrder = 2, IsActive = true });
        context.ModuleActionPermissions.AddRange(
            new ModuleActionPermission { Id = 100, ModulePermissionId = 1, ActionKey = "a100", DisplayName = "A100", DisplayOrder = 1, IsActive = true },
            new ModuleActionPermission { Id = 101, ModulePermissionId = 2, ActionKey = "a101", DisplayName = "A101", DisplayOrder = 2, IsActive = true });
        context.Set<ApplicationRole>().Add(role);
        await context.SaveChangesAsync();

        var permissionService = new Mock<IPermissionService>();
        var controller = CreateController(context, permissionService.Object, withUserId: 42);

        var dto = new UpdateRoleModulesDto
        {
            RoleId = role.Id,
            ModulePermissionIds = new List<int> { 1, 2 },
            ModuleActionPermissionIds = new List<int> { 100, 101 }
        };

        var result = await controller.UpdateRoleModules(role.Id, dto);

        result.Should().BeOfType<OkResult>();
        permissionService.Verify(
            p => p.UpdateRoleModulesAsync(role.Id, It.Is<IEnumerable<int>>(m => m.SequenceEqual(new[] { 1, 2 })), 42),
            Times.Once);
        permissionService.Verify(
            p => p.UpdateRoleModuleActionsAsync(role.Id, It.Is<IEnumerable<int>>(m => m.SequenceEqual(new[] { 100, 101 })), 42),
            Times.Once);
    }

    private static PermissionsController CreateController(ApplicationDbContext context, IPermissionService permissionService, int? withUserId = null)
    {
        var logger = Mock.Of<ILogger<PermissionsController>>();
        var controller = new PermissionsController(context, permissionService, logger);

        var identity = withUserId.HasValue
            ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, withUserId.Value.ToString()) }, "TestAuth")
            : new ClaimsIdentity();

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        return controller;
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
