using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using erp.Services.Onboarding;
using erp.DTOs.Onboarding;
using System.Security.Claims;

namespace erp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OnboardingController : ControllerBase
{
    private readonly IOnboardingService _onboardingService;

    public OnboardingController(IOnboardingService onboardingService)
    {
        _onboardingService = onboardingService;
    }

    [HttpGet("tours")]
    public ActionResult<List<OnboardingTour>> GetTours()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        var tours = _onboardingService.GetAvailableTours(userId, userRoles);
        
        return Ok(tours);
    }

    [HttpGet("tours/{tourId}")]
    public ActionResult<OnboardingTour> GetTour(string tourId)
    {
        var tour = _onboardingService.GetTourById(tourId);
        if (tour == null)
        {
            return NotFound(new { error = "Tour not found" });
        }

        return Ok(tour);
    }

    [HttpGet("progress/{tourId}")]
    public async Task<ActionResult<OnboardingProgress>> GetProgress(string tourId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var progress = await _onboardingService.GetUserProgressAsync(userId, tourId);
        if (progress == null)
        {
            return NotFound(new { error = "Progress not found" });
        }

        return Ok(progress);
    }

    [HttpPost("complete/{tourId}")]
    public async Task<ActionResult> CompleteTour(string tourId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await _onboardingService.CompleteTourAsync(userId, tourId);
        return Ok(new { message = "Tour completed successfully" });
    }

    [HttpPost("skip/{tourId}")]
    public async Task<ActionResult> SkipTour(string tourId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await _onboardingService.SkipTourAsync(userId, tourId);
        return Ok(new { message = "Tour skipped successfully" });
    }

    [HttpPost("reset/{tourId}")]
    public async Task<ActionResult> ResetTour(string tourId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await _onboardingService.ResetTourAsync(userId, tourId);
        return Ok(new { message = "Tour reset successfully" });
    }

    [HttpPost("progress/{tourId}")]
    public async Task<ActionResult> SaveProgress(string tourId, [FromBody] SaveProgressRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await _onboardingService.SaveProgressAsync(userId, tourId, request.CurrentStep, request.IsCompleted);
        return Ok(new { message = "Progress saved successfully" });
    }
}

public class SaveProgressRequest
{
    public int CurrentStep { get; set; }
    public bool IsCompleted { get; set; }
}
