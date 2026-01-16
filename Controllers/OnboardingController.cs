using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using erp.Services.Onboarding;
using erp.DTOs.Onboarding;
using System.Security.Claims;

namespace erp.Controllers;

[ApiController]
[Route("api/onboarding")]
[Authorize]
public class OnboardingController : ControllerBase
{
    private readonly IOnboardingService _onboardingService;

    public OnboardingController(IOnboardingService onboardingService)
    {
        _onboardingService = onboardingService;
    }

    /// <summary>
    /// Retorna as tours de onboarding disponíveis para o usuário autenticado.
    /// </summary>
    /// <returns>Lista de <see cref="OnboardingTour"/> que o usuário pode iniciar.</returns>
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

    /// <summary>
    /// Retorna os metadados de uma tour específica por id.
    /// </summary>
    /// <param name="tourId">Identificador da tour.</param>
    /// <returns>Objeto <see cref="OnboardingTour"/> ou 404 se não encontrado.</returns>
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

    /// <summary>
    /// Recupera o progresso do usuário para a tour especificada.
    /// </summary>
    /// <param name="tourId">Identificador da tour.</param>
    /// <returns>Objeto <see cref="OnboardingProgress"/> ou 404 se não existir.</returns>
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

    /// <summary>
    /// Marca uma tour como concluída para o usuário autenticado.
    /// </summary>
    /// <param name="tourId">Identificador da tour a ser concluída.</param>
    /// <returns>Resposta HTTP 200 em caso de sucesso.</returns>
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

    /// <summary>
    /// Marca a tour como pulada pelo usuário autenticado.
    /// </summary>
    /// <param name="tourId">Identificador da tour a ser pulada.</param>
    /// <returns>Resposta HTTP 200 em caso de sucesso.</returns>
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

    /// <summary>
    /// Restaura o estado da tour para o usuário (reseta progresso).
    /// </summary>
    /// <param name="tourId">Identificador da tour a resetar.</param>
    /// <returns>Resposta HTTP 200 em caso de sucesso.</returns>
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

    /// <summary>
    /// Salva o progresso do usuário em uma tour específica.
    /// </summary>
    /// <param name="tourId">Identificador da tour.</param>
    /// <param name="request">Objeto com o passo atual e se está completo.</param>
    /// <returns>Resposta HTTP 200 em caso de sucesso.</returns>
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
    /// <summary>Passo atual da tour que o usuário alcançou.</summary>
    public int CurrentStep { get; set; }

    /// <summary>Indica se a tour está marcada como concluída.</summary>
    public bool IsCompleted { get; set; }
}
