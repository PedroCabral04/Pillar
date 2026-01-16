using System.Text;
using System.Text.Encodings.Web;
using erp.DTOs.Auth;
using erp.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace erp.Controllers;

[ApiController]
[Route("api/dois-fatores")]
[Authorize]
public class TwoFactorController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<TwoFactorController> _logger;
    private readonly UrlEncoder _urlEncoder;

    private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

    public TwoFactorController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<TwoFactorController> logger,
        UrlEncoder urlEncoder)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _urlEncoder = urlEncoder;
    }

    /// <summary>
    /// Obtém o status do 2FA para o usuário atual
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<TwoFactorStatusResponse>> GetStatus()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized(new { message = "Usuário não encontrado." });

        var isTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        var authenticatorKey = await _userManager.GetAuthenticatorKeyAsync(user);
        var hasAuthenticator = !string.IsNullOrWhiteSpace(authenticatorKey);
        var recoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user);
        var isMachineRemembered = await _signInManager.IsTwoFactorClientRememberedAsync(user);

        return Ok(new TwoFactorStatusResponse
        {
            IsTwoFactorEnabled = isTwoFactorEnabled,
            HasAuthenticator = hasAuthenticator,
            RecoveryCodesLeft = recoveryCodesLeft,
            IsMachineRemembered = isMachineRemembered
        });
    }

    /// <summary>
    /// Inicia o processo de habilitação do 2FA - retorna QR code e chave secreta
    /// </summary>
    [HttpPost("enable")]
    public async Task<ActionResult<EnableTwoFactorResponse>> EnableTwoFactor()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized(new { message = "Usuário não encontrado." });

        var alreadyEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        if (alreadyEnabled)
        {
            // Se já habilitado, não resetamos a chave automaticamente para evitar perda acidental
            var existingKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrWhiteSpace(existingKey))
            {
                // Chave ausente num estado habilitado -> regenerar por segurança
                await _userManager.ResetAuthenticatorKeyAsync(user);
                existingKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }
            var emailExisting = await _userManager.GetEmailAsync(user);
            var uriExisting = GenerateQrCodeUri(emailExisting!, existingKey!);
            var qrExisting = GenerateQrCodeBase64(uriExisting);
            _logger.LogInformation("User {UserId} requested existing 2FA setup data (2FA already enabled)", user.Id);
            return Ok(new EnableTwoFactorResponse
            {
                SharedKey = FormatKey(existingKey!),
                AuthenticatorUri = uriExisting,
                QrCodeBase64 = qrExisting
            });
        }

        // Reseta a chave do authenticator para começar novo processo (estado desabilitado)
        await _userManager.ResetAuthenticatorKeyAsync(user);
        var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);

        if (string.IsNullOrEmpty(unformattedKey))
        {
            _logger.LogError("Failed to generate authenticator key for user {UserId}", user.Id);
            return StatusCode(500, new { message = "Erro ao gerar chave do autenticador." });
        }

        var email = await _userManager.GetEmailAsync(user);
        var authenticatorUri = GenerateQrCodeUri(email!, unformattedKey);
        var qrCodeBase64 = GenerateQrCodeBase64(authenticatorUri);

        _logger.LogInformation("User {UserId} started 2FA setup", user.Id);

        return Ok(new EnableTwoFactorResponse
        {
            SharedKey = FormatKey(unformattedKey),
            AuthenticatorUri = authenticatorUri,
            QrCodeBase64 = qrCodeBase64
        });
    }

    /// <summary>
    /// Verifica o código do authenticator e completa a habilitação do 2FA
    /// </summary>
    [HttpPost("verify-setup")]
    public async Task<ActionResult<RecoveryCodesResponse>> VerifySetup([FromBody] VerifyTwoFactorSetupRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized(new { message = "Usuário não encontrado." });

        // Idempotência: se já estiver habilitado, não tentar validar novamente; apenas retornar códigos restantes
        var alreadyEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        if (alreadyEnabled)
        {
            var codesLeft = await _userManager.CountRecoveryCodesAsync(user);
            return Ok(new RecoveryCodesResponse
            {
                RecoveryCodes = new List<string>(), // Não reexibimos códigos antigos
            });
        }

        // Remove espaços e traços do código
        var verificationCode = request.Code.Replace(" ", string.Empty).Replace("-", string.Empty);

        // Verifica o código do authenticator
        var is2faTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
            user, 
            _userManager.Options.Tokens.AuthenticatorTokenProvider, 
            verificationCode);

        if (!is2faTokenValid)
        {
            _logger.LogWarning("Invalid 2FA verification code for user {UserId}", user.Id);
            return BadRequest(new { message = "Código de verificação inválido." });
        }

        // Habilita o 2FA
        await _userManager.SetTwoFactorEnabledAsync(user, true);

        // Gera códigos de recuperação
    var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

        _logger.LogInformation("User {UserId} successfully enabled 2FA", user.Id);

        return Ok(new RecoveryCodesResponse
        {
            RecoveryCodes = recoveryCodes?.ToList() ?? new List<string>()
        });
    }

    /// <summary>
    /// Gera novos códigos de recuperação (requer 2FA já habilitado)
    /// </summary>
    [HttpPost("recovery-codes")]
    public async Task<ActionResult<RecoveryCodesResponse>> GenerateRecoveryCodes()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized(new { message = "Usuário não encontrado." });

        var isTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        if (!isTwoFactorEnabled)
            return BadRequest(new { message = "2FA não está habilitado para este usuário." });

    var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

    _logger.LogInformation("User {UserId} generated new recovery codes (previous codes invalidated)", user.Id);

        return Ok(new RecoveryCodesResponse
        {
            RecoveryCodes = recoveryCodes?.ToList() ?? new List<string>()
        });
    }

    /// <summary>
    /// Desabilita o 2FA para o usuário atual
    /// </summary>
    [HttpPost("disable")]
    public async Task<ActionResult<TwoFactorSuccessResponse>> DisableTwoFactor()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized(new { message = "Usuário não encontrado." });

        var disable2faResult = await _userManager.SetTwoFactorEnabledAsync(user, false);
        if (!disable2faResult.Succeeded)
        {
            _logger.LogError("Failed to disable 2FA for user {UserId}", user.Id);
            return StatusCode(500, new { message = "Erro ao desabilitar 2FA." });
        }

        // Reseta a chave do authenticator
        await _userManager.ResetAuthenticatorKeyAsync(user);

    _logger.LogInformation("User {UserId} disabled 2FA (authenticator key reset)", user.Id);

        return Ok(new TwoFactorSuccessResponse
        {
            Message = "Autenticação de dois fatores desabilitada com sucesso."
        });
    }

    /// <summary>
    /// Remove a lembrança do dispositivo atual
    /// </summary>
    [HttpPost("forget-machine")]
    public async Task<ActionResult<TwoFactorSuccessResponse>> ForgetMachine()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized(new { message = "Usuário não encontrado." });

        await _signInManager.ForgetTwoFactorClientAsync();

        _logger.LogInformation("User {UserId} forgot this machine for 2FA", user.Id);

        return Ok(new TwoFactorSuccessResponse
        {
            Message = "Este dispositivo foi esquecido. Você precisará fornecer um código 2FA no próximo login."
        });
    }

    #region Helper Methods

    private string FormatKey(string unformattedKey)
    {
        var result = new StringBuilder();
        int currentPosition = 0;
        while (currentPosition + 4 < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
            currentPosition += 4;
        }
        if (currentPosition < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition));
        }

        return result.ToString().ToLowerInvariant();
    }

    private string GenerateQrCodeUri(string email, string unformattedKey)
    {
        return string.Format(
            AuthenticatorUriFormat,
            _urlEncoder.Encode("Pillar ERP"),
            _urlEncoder.Encode(email),
            unformattedKey);
    }

    private string GenerateQrCodeBase64(string textToEncode)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(textToEncode, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeImage = qrCode.GetGraphic(20);
        return Convert.ToBase64String(qrCodeImage);
    }

    #endregion
}
