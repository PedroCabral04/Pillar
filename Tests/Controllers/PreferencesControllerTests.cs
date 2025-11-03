using System.Security.Claims;
using System.Text.Json;
using erp.Controllers;
using erp.DTOs.Preferences;
using erp.Models.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace erp.Tests.Controllers;

/// <summary>
/// Testes unitários para o controlador de preferências
/// </summary>
public class PreferencesControllerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly PreferencesController _controller;
    private readonly ApplicationUser _testUser;

    public PreferencesControllerTests()
    {
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null, null, null, null, null, null, null, null);

        _testUser = new ApplicationUser
        {
            Id = 1,
            UserName = "testuser",
            Email = "test@test.com"
        };

        _controller = new PreferencesController(_mockUserManager.Object);

        // Setup user context
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "test@test.com")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task GetMy_WithEmptyPreferences_ReturnsDefaultPreferences()
    {
        // Arrange
        _testUser.PreferencesJson = null;
        _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(_testUser);

        // Act
        var result = await _controller.GetMy();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var prefs = okResult.Value as UserPreferences;
        prefs.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMy_WithExistingPreferences_ReturnsPreferences()
    {
        // Arrange
        var expectedPrefs = new UserPreferences
        {
            Ui = new UiPreferences { DarkMode = true },
            Locale = new LocalePreferences { Language = "pt-BR" }
        };
        _testUser.PreferencesJson = JsonSerializer.Serialize(expectedPrefs);
        
        _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(_testUser);

        // Act
        var result = await _controller.GetMy();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var prefs = okResult.Value as UserPreferences;
        prefs.Should().NotBeNull();
        prefs!.Ui.DarkMode.Should().BeTrue();
        prefs.Locale.Language.Should().Be("pt-BR");
    }

    [Fact]
    public async Task GetMy_WithNoUser_ReturnsUnauthorized()
    {
        // Arrange
        _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _controller.GetMy();

        // Assert
        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task UpdateMy_WithValidPreferences_ReturnsNoContent()
    {
        // Arrange
        _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(_testUser);

        _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var prefs = new UserPreferences
        {
            Ui = new UiPreferences { DarkMode = false },
            Locale = new LocalePreferences { Language = "en-US" }
        };

        // Act
        var result = await _controller.UpdateMy(prefs);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _testUser.PreferencesJson.Should().NotBeNullOrEmpty();
        _mockUserManager.Verify(x => x.UpdateAsync(_testUser), Times.Once);
    }

    [Fact]
    public async Task UpdateMy_WithNoUser_ReturnsUnauthorized()
    {
        // Arrange
        _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser?)null);

        var prefs = new UserPreferences();

        // Act
        var result = await _controller.UpdateMy(prefs);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task UpdateMy_WithFailedUpdate_ReturnsBadRequest()
    {
        // Arrange
        _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(_testUser);

        _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Update failed" }));

        var prefs = new UserPreferences();

        // Act
        var result = await _controller.UpdateMy(prefs);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
