using System.Security.Claims;
using erp.Controllers;
using erp.DTOs.User;
using erp.Models.Identity;
using erp.Services.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace erp.Tests.Controllers;

/// <summary>
/// Testes unitários para o controlador de usuários
/// Cobre CRUD, validação de email/username e gerenciamento de roles
/// </summary>
public class UserControllerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<RoleManager<ApplicationRole>> _mockRoleManager;
    private readonly Mock<ITenantContextAccessor> _mockTenantAccessor;
    private readonly UsersController _controller;

    private readonly Mock<erp.Data.ApplicationDbContext> _mockContext;
    private readonly TenantContext _tenantContext;
    
    public UserControllerTests()
    {
        // Setup UserManager mock
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        // Setup RoleManager mock
        var roleStore = new Mock<IRoleStore<ApplicationRole>>();
        _mockRoleManager = new Mock<RoleManager<ApplicationRole>>(
            roleStore.Object, null!, null!, null!, null!);
        
        // Setup DbContext mock - apenas mock simples, sem banco real
        _mockContext = new Mock<erp.Data.ApplicationDbContext>();

        _mockTenantAccessor = new Mock<ITenantContextAccessor>();
        _tenantContext = new TenantContext();
        _mockTenantAccessor.SetupGet(x => x.Current).Returns(_tenantContext);

        _controller = new UsersController(
            _mockUserManager.Object,
            _mockRoleManager.Object,
            _mockContext.Object,
            _mockTenantAccessor.Object);

        SetUsersQueryable(Array.Empty<ApplicationUser>());

        // Setup user context
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "admin@test.com"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    private void SetUsersQueryable(IEnumerable<ApplicationUser> users)
    {
        _mockUserManager.Setup(x => x.Users).Returns(users.AsQueryable());
    }

    private void SetTenantClaim(int tenantId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"),
            new(ClaimTypes.Name, "admin@test.com"),
            new(ClaimTypes.Role, "Admin"),
            new(TenantClaimTypes.TenantId, tenantId.ToString())
        };

        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"))
        };
    }

    #region GetAllUsers Tests

    [Fact]
    public async Task GetAllUsers_ReturnsOkWithUserList()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new ApplicationUser { Id = 1, UserName = "user1", Email = "user1@test.com", IsActive = true },
            new ApplicationUser { Id = 2, UserName = "user2", Email = "user2@test.com", IsActive = true }
        };

        SetUsersQueryable(users);

        _mockUserManager.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "User" });

        // Act
        var result = await _controller.GetAllUsers();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedUsers = okResult.Value as List<UserDto>;
        returnedUsers.Should().NotBeNull();
        returnedUsers.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllUsers_WithTenantClaim_FiltersOtherTenants()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { Id = 1, UserName = "t1", Email = "t1@test.com", TenantId = 10 },
            new() { Id = 2, UserName = "t2", Email = "t2@test.com", TenantId = 20 }
        };

        SetUsersQueryable(users);
        SetTenantClaim(10);

        _mockUserManager.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _controller.GetAllUsers();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedUsers = okResult.Value as List<UserDto>;
        returnedUsers.Should().NotBeNull();
        returnedUsers!.Should().HaveCount(1);
        returnedUsers[0].Id.Should().Be(1);
    }

    #endregion

    #region GetUserById Tests

    [Fact]
    public async Task GetUserById_WithExistingId_ReturnsOk()
    {
        // Arrange
        var userId = 1;
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "testuser",
            Email = "test@test.com",
            PhoneNumber = "123456789",
            IsActive = true
        };

        SetUsersQueryable(new List<ApplicationUser> { user });

        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });

        // Act
        var result = await _controller.GetUserById(userId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var userDto = okResult.Value as UserDto;
        userDto.Should().NotBeNull();
        userDto!.Id.Should().Be(userId);
        userDto.Username.Should().Be("testuser");
        userDto.Email.Should().Be("test@test.com");
    }

    [Fact]
    public async Task GetUserById_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var userId = 999;
        SetUsersQueryable(Array.Empty<ApplicationUser>());

        // Act
        var result = await _controller.GetUserById(userId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetUserById_WithDifferentTenant_ReturnsNotFound()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = 5,
            UserName = "other",
            Email = "other@test.com",
            TenantId = 20
        };

        SetUsersQueryable(new List<ApplicationUser> { user });
        SetTenantClaim(10);

        // Act
        var result = await _controller.GetUserById(user.Id);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region CreateUser Tests

    [Fact]
    public async Task CreateUser_WithValidData_ReturnsCreated()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            Username = "newuser",
            Email = "newuser@test.com",
            Phone = "123456789",
            Password = "NewUser@123!",
            RoleIds = new List<int> { 1 }
        };

        var roles = new List<ApplicationRole>
        {
            new ApplicationRole { Id = 1, Name = "User" }
        };

        _mockRoleManager.Setup(x => x.Roles)
            .Returns(roles.AsQueryable());

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.AddToRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.CreateUser(createDto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var userDto = createdResult.Value as UserDto;
        userDto.Should().NotBeNull();
        userDto!.Username.Should().Be("newuser");
        userDto.Email.Should().Be("newuser@test.com");
    }

    [Fact]
    public async Task CreateUser_WithoutRoles_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            Username = "newuser",
            Email = "newuser@test.com",
            RoleIds = new List<int>()
        };

        // Act
        var result = await _controller.CreateUser(createDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateUser_WithFailedCreation_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            Username = "newuser",
            Email = "newuser@test.com",
            Password = "NewUser@123!",
            RoleIds = new List<int> { 1 }
        };

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "User creation failed" }));

        // Act
        var result = await _controller.CreateUser(createDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region UpdateUser Tests

    [Fact]
    public async Task UpdateUser_WithValidData_ReturnsNoContent()
    {
        // Arrange
        var userId = 1;
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "testuser",
            Email = "test@test.com",
            IsActive = true
        };

        var updateDto = new UpdateUserDto
        {
            Username = "updateduser",
            Email = "updated@test.com",
            Phone = "987654321",
            IsActive = true,
            RoleIds = new List<int> { 1 }
        };

        var roles = new List<ApplicationRole>
        {
            new ApplicationRole { Id = 1, Name = "User" }
        };

        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mockRoleManager.Setup(x => x.Roles)
            .Returns(roles.AsQueryable());

        _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Admin" });

        _mockUserManager.Setup(x => x.AddToRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.RemoveFromRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.UpdateUser(userId, updateDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateUser_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var userId = 999;
        var updateDto = new UpdateUserDto
        {
            Username = "updateduser",
            Email = "updated@test.com",
            RoleIds = new List<int> { 1 }
        };

        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _controller.UpdateUser(userId, updateDto);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateUser_WithPassword_UpdatesPassword()
    {
        // Arrange
        var userId = 1;
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "testuser",
            Email = "test@test.com",
            IsActive = true
        };

        var updateDto = new UpdateUserDto
        {
            Username = "testuser",
            Email = "test@test.com",
            Password = "NewPassword@123!",
            RoleIds = new List<int> { 1 }
        };

        var roles = new List<ApplicationRole>
        {
            new ApplicationRole { Id = 1, Name = "User" }
        };

        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mockRoleManager.Setup(x => x.Roles)
            .Returns(roles.AsQueryable());

        _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });

        _mockUserManager.Setup(x => x.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync("reset-token");

        _mockUserManager.Setup(x => x.ResetPasswordAsync(user, "reset-token", updateDto.Password))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.UpdateUser(userId, updateDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockUserManager.Verify(x => x.GeneratePasswordResetTokenAsync(user), Times.Once);
        _mockUserManager.Verify(x => x.ResetPasswordAsync(user, "reset-token", updateDto.Password), Times.Once);
    }

    #endregion

    #region DeleteUser Tests

    [Fact]
    public async Task DeleteUser_WithExistingId_ReturnsNoContent()
    {
        // Arrange
        var userId = 1;
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "testuser",
            Email = "test@test.com"
        };

        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mockUserManager.Setup(x => x.DeleteAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.DeleteUser(userId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockUserManager.Verify(x => x.DeleteAsync(user), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var userId = 999;
        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _controller.DeleteUser(userId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteUser_WithFailedDeletion_ReturnsBadRequest()
    {
        // Arrange
        var userId = 1;
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "testuser",
            Email = "test@test.com"
        };

        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mockUserManager.Setup(x => x.DeleteAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Cannot delete user" }));

        // Act
        var result = await _controller.DeleteUser(userId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region ValidateEmail Tests

    [Fact]
    public async Task ValidateEmail_WithAvailableEmail_ReturnsOk()
    {
        // Arrange
        var email = "available@test.com";
        SetUsersQueryable(Array.Empty<ApplicationUser>());

        // Act
        var result = await _controller.ValidateEmail(email);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value as ValidationResponse;
        response.Should().NotBeNull();
        response!.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateEmail_WithTakenEmail_ReturnsConflict()
    {
        // Arrange
        var email = "taken@test.com";
        var existingUser = new ApplicationUser
        {
            Id = 1,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant()
        };
        SetUsersQueryable(new List<ApplicationUser> { existingUser });

        // Act
        var result = await _controller.ValidateEmail(email);

        // Assert
        var conflictResult = result.Result.Should().BeOfType<ConflictObjectResult>().Subject;
        var response = conflictResult.Value as ValidationResponse;
        response.Should().NotBeNull();
        response!.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateEmail_WithEmptyEmail_ReturnsBadRequest()
    {
        // Arrange
        var email = "";

        // Act
        var result = await _controller.ValidateEmail(email);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ValidateEmail_IgnoresUsersFromOtherTenant()
    {
        // Arrange
        var email = "tenant@test.com";
        var otherTenantUser = new ApplicationUser
        {
            Id = 10,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            TenantId = 20
        };

        SetUsersQueryable(new List<ApplicationUser> { otherTenantUser });
        SetTenantClaim(10);

        // Act
        var result = await _controller.ValidateEmail(email);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value as ValidationResponse;
        response.Should().NotBeNull();
        response!.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateEmail_WithExcludedUserId_ReturnsOk()
    {
        // Arrange
        var email = "test@test.com";
        var userId = 1;
        var existingUser = new ApplicationUser
        {
            Id = userId,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant()
        };
        SetUsersQueryable(new List<ApplicationUser> { existingUser });

        // Act
        var result = await _controller.ValidateEmail(email, userId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value as ValidationResponse;
        response.Should().NotBeNull();
        response!.IsAvailable.Should().BeTrue();
    }

    #endregion

    #region ValidateUsername Tests

    [Fact]
    public async Task ValidateUsername_WithAvailableUsername_ReturnsOk()
    {
        // Arrange
        var username = "availableuser";
        SetUsersQueryable(Array.Empty<ApplicationUser>());

        // Act
        var result = await _controller.ValidateUsername(username);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value as ValidationResponse;
        response.Should().NotBeNull();
        response!.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateUsername_WithTakenUsername_ReturnsConflict()
    {
        // Arrange
        var username = "takenuser";
        var existingUser = new ApplicationUser
        {
            Id = 1,
            UserName = username,
            NormalizedUserName = username.ToUpperInvariant()
        };
        SetUsersQueryable(new List<ApplicationUser> { existingUser });

        // Act
        var result = await _controller.ValidateUsername(username);

        // Assert
        var conflictResult = result.Result.Should().BeOfType<ConflictObjectResult>().Subject;
        var response = conflictResult.Value as ValidationResponse;
        response.Should().NotBeNull();
        response!.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateUsername_WithEmptyUsername_ReturnsBadRequest()
    {
        // Arrange
        var username = "";

        // Act
        var result = await _controller.ValidateUsername(username);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ValidateUsername_WithExcludedUserId_ReturnsOk()
    {
        // Arrange
        var username = "testuser";
        var userId = 1;
        var existingUser = new ApplicationUser
        {
            Id = userId,
            UserName = username,
            NormalizedUserName = username.ToUpperInvariant()
        };
        SetUsersQueryable(new List<ApplicationUser> { existingUser });

        // Act
        var result = await _controller.ValidateUsername(username, userId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value as ValidationResponse;
        response.Should().NotBeNull();
        response!.IsAvailable.Should().BeTrue();
    }

    #endregion
}
