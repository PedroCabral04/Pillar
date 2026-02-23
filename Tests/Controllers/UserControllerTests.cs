using System.Security.Claims;
using erp.Controllers;
using erp.Data;
using erp.DTOs.User;
using erp.Models.Identity;
using erp.Services.Tenancy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace erp.Tests.Controllers;

public class UserControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<UserManager<ApplicationUser>> _userManager;
    private readonly Mock<RoleManager<ApplicationRole>> _roleManager;
    private readonly Mock<ITenantContextAccessor> _tenantAccessor;
    private readonly Mock<ILogger<UsersController>> _logger;
    private readonly UsersController _controller;

    public UserControllerTests()
    {
        _tenantAccessor = new Mock<ITenantContextAccessor>();
        _tenantAccessor.SetupGet(x => x.Current).Returns(new TenantContext());

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options, tenantContextAccessor: _tenantAccessor.Object);

        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _userManager = new Mock<UserManager<ApplicationUser>>(
            userStore.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);

        var roleStore = new Mock<IRoleStore<ApplicationRole>>();
        _roleManager = new Mock<RoleManager<ApplicationRole>>(
            roleStore.Object,
            null!,
            null!,
            null!,
            null!);

        _logger = new Mock<ILogger<UsersController>>();

        _userManager.SetupGet(x => x.Users).Returns(_context.Users);

        _controller = new UsersController(
            _userManager.Object,
            _roleManager.Object,
            _context,
            _tenantAccessor.Object,
            _logger.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, "1")],
                    "mock"))
            }
        };
    }

    [Fact]
    public async Task ValidateEmail_WhenAlreadyUsed_ReturnsConflict()
    {
        _context.Users.Add(new ApplicationUser
        {
            Id = 10,
            UserName = "joao",
            Email = "joao@erp.local",
            NormalizedEmail = "JOAO@ERP.LOCAL"
        });
        await _context.SaveChangesAsync();

        var result = await _controller.ValidateEmail("joao@erp.local");

        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task ValidateUsername_WhenExcludedUserMatches_ReturnsOk()
    {
        _context.Users.Add(new ApplicationUser
        {
            Id = 20,
            UserName = "maria",
            NormalizedUserName = "MARIA",
            Email = "maria@erp.local"
        });
        await _context.SaveChangesAsync();

        var result = await _controller.ValidateUsername("maria", 20);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<ValidationResponse>().Subject;
        payload.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task CreateUser_WithoutRoles_ReturnsBadRequest()
    {
        var result = await _controller.CreateUser(new CreateUserDto
        {
            Username = "novo",
            Email = "novo@erp.local",
            RoleIds = []
        });

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetUserById_WhenNotFound_ReturnsNotFound()
    {
        var result = await _controller.GetUserById(999);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
