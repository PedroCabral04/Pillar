using System.Security.Claims;
using System.Web;
using erp.DTOs.Auth;
using erp.Services.Email;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Identity;
using erp.Models.Identity;

namespace erp.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    SignInManager<ApplicationUser> signInManager, 
    UserManager<ApplicationUser> userManager,
    IEmailService emailService,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("login")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Email e senha são obrigatórios.");

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Unauthorized("Credenciais inválidas.");

        var result = await signInManager.PasswordSignInAsync(user, request.Password, request.RememberMe, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
                return Unauthorized("Conta bloqueada temporariamente.");
            return Unauthorized("Credenciais inválidas.");
        }

        return Ok(new { message = "Autenticado", user = new { user.Id, user.UserName, user.Email } });
    }

    [HttpPost("logout")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return Ok(new { message = "Desconectado" });
    }

    /// <summary>
    /// Solicita recuperação de senha - envia email com token
    /// </summary>
    [HttpPost("password-reset/request")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await userManager.FindByEmailAsync(request.Email);
        
        // Por segurança, sempre retorna sucesso mesmo se o usuário não existir
        // Isso previne enumeração de usuários
        if (user is null)
        {
            logger.LogWarning("Password reset requested for non-existent email: {Email}", request.Email);
            return Ok(new { message = "Se o email estiver cadastrado, você receberá instruções para redefinir sua senha." });
        }

        // Gera token de reset
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        
        // Constrói URL de reset (ajuste conforme sua aplicação)
        var resetUrl = $"{Request.Scheme}://{Request.Host}/reset-password?email={HttpUtility.UrlEncode(request.Email)}&token={HttpUtility.UrlEncode(resetToken)}";

        // Envia email
        var emailSent = await emailService.SendPasswordResetEmailAsync(
            user.Email!, 
            user.UserName ?? user.Email!, 
            resetToken, 
            resetUrl);

        if (!emailSent)
        {
            logger.LogError("Failed to send password reset email to: {Email}", request.Email);
            return StatusCode(500, new { message = "Erro ao enviar email. Tente novamente mais tarde." });
        }

        logger.LogInformation("Password reset email sent to: {Email}", request.Email);
        return Ok(new { message = "Se o email estiver cadastrado, você receberá instruções para redefinir sua senha." });
    }

    /// <summary>
    /// Confirma redefinição de senha com token
    /// </summary>
    [HttpPost("password-reset/confirm")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ConfirmPasswordReset([FromBody] PasswordResetConfirmRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return BadRequest(new { message = "Solicitação inválida." });

        // Reseta a senha usando o token
        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            logger.LogWarning("Password reset failed for {Email}: {Errors}", request.Email, string.Join(", ", errors));
            return BadRequest(new { message = "Erro ao redefinir senha.", errors });
        }

        logger.LogInformation("Password successfully reset for: {Email}", request.Email);
        
        // Opcional: Enviar email de confirmação
        await emailService.SendNotificationEmailAsync(
            user.Email!,
            user.UserName ?? user.Email!,
            "Senha Alterada",
            "Sua senha foi alterada com sucesso. Se você não realizou esta ação, entre em contato imediatamente com o suporte.");

        return Ok(new { message = "Senha redefinida com sucesso." });
    }

    /// <summary>
    /// Verifica se o token de reset é válido (opcional, para UX)
    /// </summary>
    [HttpPost("password-reset/verify-token")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> VerifyResetToken([FromBody] VerifyTokenRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Token))
            return BadRequest(new { message = "Email e token são obrigatórios." });

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return BadRequest(new { valid = false, message = "Token inválido ou expirado." });

        // Verifica se o token é válido
        var isValid = await userManager.VerifyUserTokenAsync(
            user, 
            userManager.Options.Tokens.PasswordResetTokenProvider, 
            "ResetPassword", 
            request.Token);

        return Ok(new { valid = isValid, message = isValid ? "Token válido." : "Token inválido ou expirado." });
    }
}

/// <summary>
/// Request para verificar validade do token
/// </summary>
public class VerifyTokenRequest
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
