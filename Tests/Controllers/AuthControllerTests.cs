using erp.Controllers;
using erp.DTOs.Auth;
using erp.Models.Identity;
using erp.Security;
using erp.Services.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Moq;
using Xunit;
using FluentAssertions;

namespace erp.Tests.Controllers;

/// <summary>
/// Testes unitários para o controlador de autenticação
/// </summary>
public class AuthControllerTests
{
    private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        // Required collaborators for UserManager
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        var identityOptions = new Mock<IOptions<IdentityOptions>>();
        identityOptions.Setup(o => o.Value).Returns(new IdentityOptions());
        var passwordHasher = new Mock<IPasswordHasher<ApplicationUser>>();
        var userValidators = new List<IUserValidator<ApplicationUser>>();
        var passwordValidators = new List<IPasswordValidator<ApplicationUser>>();
        var keyNormalizer = new Mock<ILookupNormalizer>();
        var errors = new IdentityErrorDescriber();
        var services = new Mock<IServiceProvider>();
        var userLogger = new Mock<ILogger<UserManager<ApplicationUser>>>();

        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStore.Object,
            identityOptions.Object,
            passwordHasher.Object,
            userValidators,
            passwordValidators,
            keyNormalizer.Object,
            errors,
            services.Object,
            userLogger.Object);

        // Required collaborators for SignInManager
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        var signInOptions = identityOptions; // reuse
        var signInLogger = new Mock<ILogger<SignInManager<ApplicationUser>>>();
        var schemes = new Mock<IAuthenticationSchemeProvider>();
        var confirmation = new Mock<IUserConfirmation<ApplicationUser>>();

        _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
            _mockUserManager.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            signInOptions.Object,
            signInLogger.Object,
            schemes.Object,
            confirmation.Object);

        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<AuthController>>();

        _controller = new AuthController(
            _mockSignInManager.Object, 
            _mockUserManager.Object,
            _mockEmailService.Object,
            _mockLogger.Object);
    }

    #region Login Tests

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOk()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@test.com",
            Password = "Test@123!",
            RememberMe = false
        };

        var user = new ApplicationUser
        {
            Id = 1,
            UserName = "testuser",
            Email = request.Email
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _mockSignInManager.Setup(x => x.PasswordSignInAsync(
                user,
                request.Password,
                request.RememberMe,
                true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Login_WithEmptyEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "",
            Password = "Test@123!"
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Login_WithEmptyPassword_ReturnsBadRequest()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@test.com",
            Password = ""
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Login_WithNonExistingUser_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexisting@test.com",
            Password = "Test@123!"
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@test.com",
            Password = "WrongPassword123!"
        };

        var user = new ApplicationUser
        {
            Id = 1,
            Email = request.Email
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _mockSignInManager.Setup(x => x.PasswordSignInAsync(
                user,
                request.Password,
                request.RememberMe,
                true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_WithLockedOutAccount_ReturnsUnauthorizedWithMessage()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@test.com",
            Password = "Test@123!"
        };

        var user = new ApplicationUser
        {
            Id = 1,
            Email = request.Email
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _mockSignInManager.Setup(x => x.PasswordSignInAsync(
                user,
                request.Password,
                request.RememberMe,
                true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.Value.Should().Be("Conta bloqueada temporariamente.");
    }

    [Fact]
    public async Task Login_WithRememberMe_PassesRememberMeToSignIn()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@test.com",
            Password = "Test@123!",
            RememberMe = true
        };

        var user = new ApplicationUser
        {
            Id = 1,
            Email = request.Email
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _mockSignInManager.Setup(x => x.PasswordSignInAsync(
                user,
                request.Password,
                true, // RememberMe
                true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockSignInManager.Verify(x => x.PasswordSignInAsync(
            user,
            request.Password,
            true,
            true), Times.Once);
    }

    #endregion

    #region Logout Tests

    [Fact]
    public async Task Logout_ReturnsOk()
    {
        // Arrange
        _mockSignInManager.Setup(x => x.SignOutAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Logout();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockSignInManager.Verify(x => x.SignOutAsync(), Times.Once);
    }

    #endregion

    #region Impersonation Tests

    [Fact]
    public async Task ImpersonateUser_WithSuperAdmin_ReturnsOk()
    {
        // Arrange
        var superAdmin = new ApplicationUser
        {
            Id = 1,
            UserName = "superadmin",
            Email = "superadmin@erp.local"
        };

        var targetUser = new ApplicationUser
        {
            Id = 2,
            UserName = "target.user",
            Email = "target@tenant.local",
            IsActive = true,
            TenantId = 77
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "1"),
                    new Claim(ClaimTypes.Role, RoleNames.SuperAdmin)
                }, "TestAuth"))
            }
        };

        _mockUserManager.Setup(x => x.FindByIdAsync("1"))
            .ReturnsAsync(superAdmin);
        _mockUserManager.Setup(x => x.IsInRoleAsync(superAdmin, RoleNames.SuperAdmin))
            .ReturnsAsync(true);
        _mockUserManager.Setup(x => x.FindByIdAsync("2"))
            .ReturnsAsync(targetUser);
        _mockSignInManager.Setup(x => x.SignOutAsync())
            .Returns(Task.CompletedTask);
        _mockSignInManager.Setup(x => x.SignInWithClaimsAsync(
                targetUser,
                false,
                It.IsAny<IEnumerable<Claim>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ImpersonateUser(2);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockSignInManager.Verify(x => x.SignInWithClaimsAsync(
            targetUser,
            false,
            It.Is<IEnumerable<Claim>>(claims =>
                claims.Any(c => c.Type == ImpersonationClaimTypes.IsImpersonating && c.Value == "true") &&
                claims.Any(c => c.Type == ImpersonationClaimTypes.ImpersonatorUserId && c.Value == "1"))), Times.Once);
    }

    [Fact]
    public async Task ImpersonateUser_WithMissingTarget_ReturnsNotFound()
    {
        // Arrange
        var superAdmin = new ApplicationUser
        {
            Id = 1,
            UserName = "superadmin",
            Email = "superadmin@erp.local"
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "1"),
                    new Claim(ClaimTypes.Role, RoleNames.SuperAdmin)
                }, "TestAuth"))
            }
        };

        _mockUserManager.Setup(x => x.FindByIdAsync("1"))
            .ReturnsAsync(superAdmin);
        _mockUserManager.Setup(x => x.IsInRoleAsync(superAdmin, RoleNames.SuperAdmin))
            .ReturnsAsync(true);
        _mockUserManager.Setup(x => x.FindByIdAsync("99"))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _controller.ImpersonateUser(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task StopImpersonation_WithImpersonationClaim_ReturnsOk()
    {
        // Arrange
        var superAdmin = new ApplicationUser
        {
            Id = 1,
            UserName = "superadmin",
            Email = "superadmin@erp.local"
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "2"),
                    new Claim(ImpersonationClaimTypes.IsImpersonating, "true"),
                    new Claim(ImpersonationClaimTypes.ImpersonatorUserId, "1")
                }, "TestAuth"))
            }
        };

        _mockUserManager.Setup(x => x.FindByIdAsync("1"))
            .ReturnsAsync(superAdmin);
        _mockUserManager.Setup(x => x.IsInRoleAsync(superAdmin, RoleNames.SuperAdmin))
            .ReturnsAsync(true);
        _mockSignInManager.Setup(x => x.SignOutAsync())
            .Returns(Task.CompletedTask);
        _mockSignInManager.Setup(x => x.SignInAsync(superAdmin, false, null))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.StopImpersonation();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockSignInManager.Verify(x => x.SignInAsync(superAdmin, false, null), Times.Once);
    }

    [Fact]
    public async Task StopImpersonation_WithoutImpersonationClaim_ReturnsBadRequest()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "1")
                }, "TestAuth"))
            }
        };

        // Act
        var result = await _controller.StopImpersonation();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Change Password Tests

    [Fact]
    public async Task ChangePassword_WithUnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "Admin@123!",
            NewPassword = "NewAdmin@123!",
            ConfirmPassword = "NewAdmin@123!"
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        // Act
        var result = await _controller.ChangePassword(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task ChangePassword_WithValidData_ReturnsOk()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "Admin@123!",
            NewPassword = "NewAdmin@123!",
            ConfirmPassword = "NewAdmin@123!"
        };

        var user = new ApplicationUser
        {
            Id = 1,
            Email = "admin@erp.local",
            UserName = "admin"
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "1")
                }, "TestAuth"))
            }
        };

        _mockUserManager.Setup(x => x.FindByIdAsync("1"))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.ChangePassword(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockUserManager.Verify(x => x.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword), Times.Once);
    }

    [Fact]
    public async Task ChangePassword_WithInvalidCurrentPassword_ReturnsBadRequest()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "Wrong@123!",
            NewPassword = "NewAdmin@123!",
            ConfirmPassword = "NewAdmin@123!"
        };

        var user = new ApplicationUser
        {
            Id = 1,
            Email = "admin@erp.local",
            UserName = "admin"
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "1")
                }, "TestAuth"))
            }
        };

        _mockUserManager.Setup(x => x.FindByIdAsync("1"))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Senha atual incorreta." }));

        // Act
        var result = await _controller.ChangePassword(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion
}
