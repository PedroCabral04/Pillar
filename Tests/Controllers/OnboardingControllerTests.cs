using System.Security.Claims;
using erp.Controllers;
using erp.DTOs.Onboarding;
using erp.Services.Onboarding;
using Microsoft.AspNetCore.Http;

namespace erp.Tests.Controllers;

/// <summary>
/// Testes unitários para o controlador de Onboarding
/// </summary>
public class OnboardingControllerTests
{
    private readonly Mock<IOnboardingService> _mockOnboardingService;
    private readonly OnboardingController _controller;
    private readonly string _testUserId = "1";

    public OnboardingControllerTests()
    {
        _mockOnboardingService = new Mock<IOnboardingService>();
        _controller = new OnboardingController(_mockOnboardingService.Object);

        // Setup user context
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId),
            new Claim(ClaimTypes.Name, "test@test.com"),
            new Claim(ClaimTypes.Role, "User")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    #region GetTours Tests

    [Fact]
    public void GetTours_ReturnsOkWithTourList()
    {
        // Arrange
        var tours = new List<OnboardingTour>
        {
            new OnboardingTour
            {
                Id = "tour1",
                Name = "Getting Started",
                Description = "Learn the basics",
                Steps = new List<OnboardingStep>()
            },
            new OnboardingTour
            {
                Id = "tour2",
                Name = "Advanced Features",
                Description = "Advanced topics",
                Steps = new List<OnboardingStep>()
            }
        };

        _mockOnboardingService
            .Setup(x => x.GetAvailableTours(_testUserId, It.IsAny<string[]>()))
            .Returns(tours);

        // Act
        var result = _controller.GetTours();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedTours = okResult.Value as List<OnboardingTour>;
        returnedTours.Should().NotBeNull();
        returnedTours.Should().HaveCount(2);
    }

    [Fact]
    public void GetTours_WithoutUserId_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

        // Act
        var result = _controller.GetTours();

        // Assert
        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    #endregion

    #region GetTour Tests

    [Fact]
    public void GetTour_WithExistingId_ReturnsOk()
    {
        // Arrange
        var tourId = "tour1";
        var tour = new OnboardingTour
        {
            Id = tourId,
            Name = "Getting Started",
            Description = "Learn the basics",
            Steps = new List<OnboardingStep>
            {
                new OnboardingStep { Id = "step1", Title = "Welcome", Description = "Welcome step", Target = "#welcome" }
            }
        };

        _mockOnboardingService
            .Setup(x => x.GetTourById(tourId))
            .Returns(tour);

        // Act
        var result = _controller.GetTour(tourId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedTour = okResult.Value as OnboardingTour;
        returnedTour.Should().NotBeNull();
        returnedTour!.Id.Should().Be(tourId);
        returnedTour.Steps.Should().HaveCount(1);
    }

    [Fact]
    public void GetTour_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var tourId = "nonexisting";
        _mockOnboardingService
            .Setup(x => x.GetTourById(tourId))
            .Returns((OnboardingTour?)null);

        // Act
        var result = _controller.GetTour(tourId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetProgress Tests

    [Fact]
    public async Task GetProgress_WithExistingProgress_ReturnsOk()
    {
        // Arrange
        var tourId = "tour1";
        var progress = new OnboardingProgress
        {
            UserId = _testUserId,
            TourId = tourId,
            CurrentStep = 2,
            IsCompleted = false
        };

        _mockOnboardingService
            .Setup(x => x.GetUserProgressAsync(_testUserId, tourId))
            .ReturnsAsync(progress);

        // Act
        var result = await _controller.GetProgress(tourId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedProgress = okResult.Value as OnboardingProgress;
        returnedProgress.Should().NotBeNull();
        returnedProgress!.TourId.Should().Be(tourId);
        returnedProgress.CurrentStep.Should().Be(2);
    }

    [Fact]
    public async Task GetProgress_WithNonExistingProgress_ReturnsNotFound()
    {
        // Arrange
        var tourId = "nonexisting";
        _mockOnboardingService
            .Setup(x => x.GetUserProgressAsync(_testUserId, tourId))
            .ReturnsAsync((OnboardingProgress?)null);

        // Act
        var result = await _controller.GetProgress(tourId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetProgress_WithoutUserId_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();
        var tourId = "tour1";

        // Act
        var result = await _controller.GetProgress(tourId);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    #endregion

    #region CompleteTour Tests

    [Fact]
    public async Task CompleteTour_ReturnsOk()
    {
        // Arrange
        var tourId = "tour1";
        _mockOnboardingService
            .Setup(x => x.CompleteTourAsync(_testUserId, tourId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.CompleteTour(tourId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockOnboardingService.Verify(x => x.CompleteTourAsync(_testUserId, tourId), Times.Once);
    }

    [Fact]
    public async Task CompleteTour_WithoutUserId_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();
        var tourId = "tour1";

        // Act
        var result = await _controller.CompleteTour(tourId);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    #endregion

    #region SkipTour Tests

    [Fact]
    public async Task SkipTour_ReturnsOk()
    {
        // Arrange
        var tourId = "tour1";
        _mockOnboardingService
            .Setup(x => x.SkipTourAsync(_testUserId, tourId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.SkipTour(tourId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockOnboardingService.Verify(x => x.SkipTourAsync(_testUserId, tourId), Times.Once);
    }

    [Fact]
    public async Task SkipTour_WithoutUserId_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();
        var tourId = "tour1";

        // Act
        var result = await _controller.SkipTour(tourId);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    #endregion

    #region ResetTour Tests

    [Fact]
    public async Task ResetTour_ReturnsOk()
    {
        // Arrange
        var tourId = "tour1";
        _mockOnboardingService
            .Setup(x => x.ResetTourAsync(_testUserId, tourId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ResetTour(tourId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockOnboardingService.Verify(x => x.ResetTourAsync(_testUserId, tourId), Times.Once);
    }

    [Fact]
    public async Task ResetTour_WithoutUserId_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();
        var tourId = "tour1";

        // Act
        var result = await _controller.ResetTour(tourId);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    #endregion

    #region SaveProgress Tests

    [Fact]
    public async Task SaveProgress_WithValidData_ReturnsOk()
    {
        // Arrange
        var tourId = "tour1";
        var request = new SaveProgressRequest
        {
            CurrentStep = 3,
            IsCompleted = false
        };

        _mockOnboardingService
            .Setup(x => x.SaveProgressAsync(_testUserId, tourId, request.CurrentStep, request.IsCompleted))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.SaveProgress(tourId, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockOnboardingService.Verify(
            x => x.SaveProgressAsync(_testUserId, tourId, request.CurrentStep, request.IsCompleted),
            Times.Once);
    }

    [Fact]
    public async Task SaveProgress_WithCompletedStatus_ReturnsOk()
    {
        // Arrange
        var tourId = "tour1";
        var request = new SaveProgressRequest
        {
            CurrentStep = 5,
            IsCompleted = true
        };

        _mockOnboardingService
            .Setup(x => x.SaveProgressAsync(_testUserId, tourId, request.CurrentStep, request.IsCompleted))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.SaveProgress(tourId, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SaveProgress_WithoutUserId_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();
        var tourId = "tour1";
        var request = new SaveProgressRequest();

        // Act
        var result = await _controller.SaveProgress(tourId, request);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    #endregion
}
