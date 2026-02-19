using System.Security.Claims;
using System.Web;
using erp.DTOs.Auth;
using erp.Security;
using erp.Services.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Identity;
using erp.Models.Identity;

namespace erp.Controllers;

/// <summary>
/// Controller responsável pela autenticação e gerenciamento de senhas
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController(
    SignInManager<ApplicationUser> signInManager, 
    UserManager<ApplicationUser> userManager,
    IEmailService emailService,
    ILogger<AuthController> logger) : ControllerBase
{
    /// <summary>
    /// Autentica um usuário no sistema
    /// </summary>
    /// <param name="request">Credenciais de login (email e senha)</param>
    /// <returns>Retorna informações do usuário autenticado</returns>
    /// <response code="200">Login realizado com sucesso</response>
    /// <response code="400">Dados de login inválidos ou ausentes</response>
    /// <response code="401">Credenciais inválidas ou conta bloqueada</response>
    /// <remarks>
    /// Exemplo de requisição:
    /// 
    ///     POST /api/auth/login
    ///     {
    ///         "email": "usuario@exemplo.com",
    ///         "password": "SenhaSegura123!",
    ///         "rememberMe": true
    ///     }
    /// 
    /// A conta será bloqueada temporariamente após 5 tentativas de login malsucedidas.
    /// </remarks>
    [HttpPost("login")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Email e senha são obrigatórios.");

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Unauthorized("Credenciais inválidas.");

        var result = await signInManager.PasswordSignInAsync(user, request.Password, request.RememberMe, lockoutOnFailure: true);
        
        if (result.RequiresTwoFactor)
        {
            // Usuário precisa fornecer código 2FA
            logger.LogInformation("User {Email} requires 2FA", request.Email);
            return Ok(new { 
                requiresTwoFactor = true, 
                message = "Autenticação de dois fatores necessária.",
                email = request.Email
            });
        }
        
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
                return Unauthorized("Conta bloqueada temporariamente.");
            return Unauthorized("Credenciais inválidas.");
        }

        return Ok(new { message = "Autenticado", user = new { user.Id, user.UserName, user.Email } });
    }

    /// <summary>
    /// Verifica o código 2FA durante o processo de login
    /// </summary>
    /// <param name="request">Código 2FA (authenticator ou recovery code)</param>
    /// <returns>Confirmação de autenticação</returns>
    /// <response code="200">Código válido - autenticado com sucesso</response>
    /// <response code="400">Dados inválidos</response>
    /// <response code="401">Código inválido ou conta bloqueada</response>
    [HttpPost("verify-2fa")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyTwoFactor([FromBody] VerifyTwoFactorLoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Verifica se há um usuário 2FA pendente
        var twoFactorUser = await signInManager.GetTwoFactorAuthenticationUserAsync();
        if (twoFactorUser == null)
        {
            // Não aborta imediatamente: algumas implementações customizadas podem ainda resolver o usuário.
            logger.LogWarning("2FA verification invoked but TwoFactorUserId cookie not found. Proceeding to attempt sign-in anyway.");
        }

        logger.LogInformation("Attempting 2FA verification (user cookie present: {HasUser})", twoFactorUser != null);

        // Remove espaços e traços do código
        var code = request.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
        logger.LogDebug("Cleaned code length: {Length}", code.Length);

        Microsoft.AspNetCore.Identity.SignInResult result;

        if (request.IsRecoveryCode)
        {
            if (twoFactorUser != null)
                logger.LogInformation("Using recovery code for user {UserId}", twoFactorUser.Id);
            // Usa código de recuperação
            result = await signInManager.TwoFactorRecoveryCodeSignInAsync(code);
            if (result.Succeeded)
            {
                if (twoFactorUser != null)
                    logger.LogInformation("User {UserId} logged in with a recovery code", twoFactorUser.Id);
            }
        }
        else
        {
            if (twoFactorUser != null)
                logger.LogInformation("Using authenticator code for user {UserId}", twoFactorUser.Id);
            // Usa código do authenticator
            // Validação simples de formato para códigos de authenticator (6 dígitos)
            if (!request.IsRecoveryCode)
            {
                var isDigitsOnly = code.All(char.IsDigit);
                if (!isDigitsOnly || (code.Length != 6 && code.Length != 7))
                {
                    logger.LogWarning("Invalid authenticator code format");
                    return BadRequest(new { message = "Código de autenticação inválido." });
                }
            }

            result = await signInManager.TwoFactorAuthenticatorSignInAsync(
                code,
                request.RememberMachine,
                rememberClient: request.RememberMachine);
            
            logger.LogDebug("2FA result - Succeeded: {Succeeded}, IsLockedOut: {IsLockedOut}, IsNotAllowed: {IsNotAllowed}", 
                result.Succeeded, result.IsLockedOut, result.IsNotAllowed);
        }

        if (result.Succeeded)
        {
            var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user != null)
            {
                logger.LogInformation("User {UserId} logged in with 2FA successfully", user.Id);
                
                return Ok(new { 
                    success = true,
                    message = "Autenticado com sucesso", 
                    redirectUrl = "/",
                    user = new { user.Id, user.UserName, user.Email } 
                });
            }
            
            logger.LogError("2FA succeeded but could not retrieve user");
            return StatusCode(500, new { message = "Erro ao processar autenticação." });
        }

        if (result.IsLockedOut)
        {
            logger.LogWarning("User account locked out after failed 2FA attempts");
            return Unauthorized(new { message = "Conta bloqueada temporariamente devido a tentativas de login malsucedidas." });
        }

        if (twoFactorUser != null)
            logger.LogWarning("Invalid 2FA code attempt for user {UserId}", twoFactorUser.Id);
        else
            logger.LogWarning("Invalid 2FA code attempt with missing TwoFactorUser context.");
        return Unauthorized(new { message = "Código de autenticação inválido." });
    }

    /// <summary>
    /// Encerra a sessão do usuário autenticado
    /// </summary>
    /// <returns>Confirmação de logout</returns>
    /// <response code="200">Logout realizado com sucesso</response>
    [HttpPost("logout")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return Ok(new { message = "Desconectado" });
    }

    [Authorize(Roles = RoleNames.SuperAdmin)]
    [HttpPost("impersonate/{userId:int}")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ImpersonateUser([FromRoute] int userId)
    {
        if (User.HasClaim(c => c.Type == ImpersonationClaimTypes.IsImpersonating &&
                               string.Equals(c.Value, "true", StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest(new { message = "Finalize a impersonação atual antes de iniciar outra." });
        }

        var superAdminIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(superAdminIdClaim, out var superAdminId))
        {
            return Unauthorized(new { message = "Usuário autenticado inválido." });
        }

        if (superAdminId == userId)
        {
            return BadRequest(new { message = "Você já está autenticado como este usuário." });
        }

        var superAdmin = await userManager.FindByIdAsync(superAdminId.ToString());
        if (superAdmin is null)
        {
            return Unauthorized(new { message = "Usuário autenticado não encontrado." });
        }

        if (!await userManager.IsInRoleAsync(superAdmin, RoleNames.SuperAdmin))
        {
            return Forbid();
        }

        var targetUser = await userManager.FindByIdAsync(userId.ToString());
        if (targetUser is null)
        {
            return NotFound(new { message = "Usuário de destino não encontrado." });
        }

        if (!targetUser.IsActive)
        {
            return BadRequest(new { message = "Não é possível impersonar um usuário inativo." });
        }

        var impersonatorName = superAdmin.UserName ?? superAdmin.Email ?? $"user-{superAdmin.Id}";
        var additionalClaims = new[]
        {
            new Claim(ImpersonationClaimTypes.IsImpersonating, "true"),
            new Claim(ImpersonationClaimTypes.ImpersonatorUserId, superAdmin.Id.ToString()),
            new Claim(ImpersonationClaimTypes.ImpersonatorUserName, impersonatorName)
        };

        await signInManager.SignOutAsync();
        await signInManager.SignInWithClaimsAsync(targetUser, isPersistent: false, additionalClaims);

        logger.LogWarning("SuperAdmin {SuperAdminId} iniciou impersonação do usuário {TargetUserId}", superAdmin.Id, targetUser.Id);

        return Ok(new
        {
            message = "Impersonação iniciada com sucesso.",
            user = new { targetUser.Id, targetUser.UserName, targetUser.Email, targetUser.TenantId }
        });
    }

    [Authorize]
    [HttpPost("stop-impersonation")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StopImpersonation()
    {
        var originalUserIdClaim = User.FindFirstValue(ImpersonationClaimTypes.ImpersonatorUserId);
        if (!int.TryParse(originalUserIdClaim, out var originalUserId))
        {
            return BadRequest(new { message = "A sessão atual não está em modo de impersonação." });
        }

        var originalUser = await userManager.FindByIdAsync(originalUserId.ToString());
        if (originalUser is null)
        {
            return NotFound(new { message = "Usuário original da sessão não foi encontrado." });
        }

        if (!await userManager.IsInRoleAsync(originalUser, RoleNames.SuperAdmin))
        {
            return Forbid();
        }

        await signInManager.SignOutAsync();
        await signInManager.SignInAsync(originalUser, isPersistent: false);

        logger.LogInformation("Impersonação encerrada. Sessão restaurada para SuperAdmin {SuperAdminId}", originalUser.Id);

        return Ok(new { message = "Impersonação encerrada com sucesso." });
    }

    /// <summary>
    /// Altera a senha do usuário autenticado
    /// </summary>
    /// <param name="request">Senha atual e nova senha</param>
    /// <returns>Confirmação de alteração ou erros de validação</returns>
    /// <response code="200">Senha alterada com sucesso</response>
    /// <response code="400">Senha atual inválida ou nova senha não atende aos requisitos</response>
    /// <response code="401">Usuário não autenticado</response>
    [Authorize]
    [HttpPost("change-password")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Usuário não autenticado." });

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return Unauthorized(new { message = "Usuário não encontrado." });

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            logger.LogWarning("Change password failed for user {UserId}: {Errors}", userId, string.Join(", ", errors));
            return BadRequest(new { message = "Erro ao alterar senha.", errors });
        }

        logger.LogInformation("Password changed successfully for user {UserId}", userId);
        return Ok(new { message = "Senha alterada com sucesso." });
    }

    /// <summary>
    /// Solicita a redefinição de senha via email
    /// </summary>
    /// <param name="request">Email do usuário que esqueceu a senha</param>
    /// <returns>Mensagem de confirmação (sempre retorna sucesso por segurança)</returns>
    /// <response code="200">Solicitação processada (email enviado se o usuário existir)</response>
    /// <response code="400">Dados da requisição inválidos</response>
    /// <response code="500">Erro ao enviar email</response>
    /// <remarks>
    /// Exemplo de requisição:
    /// 
    ///     POST /api/auth/password-reset/request
    ///     {
    ///         "email": "usuario@exemplo.com"
    ///     }
    /// 
    /// Por segurança, sempre retorna sucesso mesmo se o email não estiver cadastrado,
    /// prevenindo enumeração de usuários. O usuário receberá um email com link e token
    /// para redefinir a senha, válido por tempo limitado.
    /// </remarks>
    [HttpPost("password-reset/request")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

        // SECURITY WARNING: Token de reset sendo enviado na URL (query parameter)
        // Riscos:
        // - Tokens podem ser logados em server access logs
        // - URLs ficam no histórico do navegador
        // - URLs podem ser compartilhadas acidentalmente
        //
        // Recomendação para produção:
        // 1. Usar um token temporário (UUID curto) na URL
        // 2. Armazenar o token real no backend vinculado ao UUID
        // 3. Frontend usa o UUID para fazer POST com token real no body
        //
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
    /// Confirma a redefinição de senha usando o token recebido por email
    /// </summary>
    /// <param name="request">Email, token e nova senha</param>
    /// <returns>Confirmação de redefinição ou erros de validação</returns>
    /// <response code="200">Senha redefinida com sucesso</response>
    /// <response code="400">Token inválido, expirado ou senha não atende aos requisitos</response>
    /// <remarks>
    /// Exemplo de requisição:
    /// 
    ///     POST /api/auth/password-reset/confirm
    ///     {
    ///         "email": "usuario@exemplo.com",
    ///         "token": "CfDJ8KhJx...",
    ///         "newPassword": "NovaSenhaSegura123!"
    ///     }
    /// 
    /// Requisitos da senha:
    /// - Mínimo 8 caracteres
    /// - Pelo menos uma letra maiúscula
    /// - Pelo menos uma letra minúscula
    /// - Pelo menos um dígito
    /// - Pelo menos um caractere especial
    /// </remarks>
    [HttpPost("password-reset/confirm")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
    /// Verifica se um token de redefinição de senha é válido
    /// </summary>
    /// <param name="request">Email e token para validação</param>
    /// <returns>Indica se o token é válido</returns>
    /// <response code="200">Validação realizada (retorna status de validade)</response>
    /// <response code="400">Dados da requisição inválidos</response>
    /// <remarks>
    /// Endpoint útil para validar o token antes de exibir o formulário de nova senha,
    /// melhorando a experiência do usuário ao detectar tokens expirados antecipadamente.
    /// 
    /// Exemplo de requisição:
    /// 
    ///     POST /api/auth/password-reset/verify-token
    ///     {
    ///         "email": "usuario@exemplo.com",
    ///         "token": "CfDJ8KhJx..."
    ///     }
    /// </remarks>
    [HttpPost("password-reset/verify-token")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
