using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace erp.Controllers;

/// <summary>
/// Base controller providing common functionality for all API controllers
/// </summary>
public abstract class BaseController : ControllerBase
{
    protected ILogger Logger { get; }

    protected BaseController(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// Gets the current user ID from claims
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Thrown when user is not authenticated</exception>
    protected int CurrentUserId
    {
        get
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim?.Value == null || !int.TryParse(claim.Value, out int userId))
                throw new UnauthorizedAccessException("Usuário não autenticado");
            return userId;
        }
    }

    /// <summary>
    /// Executes an operation with consistent error handling
    /// </summary>
    protected async Task<ActionResult<T>> ExecuteAsync<T>(
        Func<Task<T>> operation,
        string errorMessage = "Erro ao processar requisição")
    {
        try
        {
            var result = await operation();
            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, errorMessage);
            return StatusCode(500, new { message = errorMessage, error = ex.Message });
        }
    }

    /// <summary>
    /// Executes an operation with consistent error handling (no return value)
    /// </summary>
    protected async Task<ActionResult> ExecuteAsync(
        Func<Task> operation,
        string errorMessage = "Erro ao processar requisição")
    {
        try
        {
            await operation();
            return Ok();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, errorMessage);
            return StatusCode(500, new { message = errorMessage, error = ex.Message });
        }
    }
}
