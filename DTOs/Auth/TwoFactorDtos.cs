using System.ComponentModel.DataAnnotations;

namespace erp.DTOs.Auth;

/// <summary>
/// Response para verificar status do 2FA
/// </summary>
public class TwoFactorStatusResponse
{
    public bool IsTwoFactorEnabled { get; set; }
    public bool HasAuthenticator { get; set; }
    public int RecoveryCodesLeft { get; set; }
    public bool IsMachineRemembered { get; set; }
}

/// <summary>
/// Request para habilitar 2FA
/// </summary>
public class EnableTwoFactorRequest
{
    // Vazio - apenas trigger para gerar QR code
}

/// <summary>
/// Response ao habilitar 2FA - inclui QR code e secret key
/// </summary>
public class EnableTwoFactorResponse
{
    public string SharedKey { get; set; } = string.Empty;
    public string AuthenticatorUri { get; set; } = string.Empty;
    public string QrCodeBase64 { get; set; } = string.Empty;
}

/// <summary>
/// Request para verificar código durante setup do 2FA
/// </summary>
public class VerifyTwoFactorSetupRequest
{
    [Required(ErrorMessage = "O código de verificação é obrigatório.")]
    [StringLength(7, ErrorMessage = "Código inválido.")]
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// Request para gerar códigos de recuperação
/// </summary>
public class GenerateRecoveryCodesRequest
{
    // Vazio - apenas trigger
}

/// <summary>
/// Response com códigos de recuperação
/// </summary>
public class RecoveryCodesResponse
{
    public List<string> RecoveryCodes { get; set; } = new();
}

/// <summary>
/// Request para verificar código 2FA durante login
/// </summary>
public class VerifyTwoFactorLoginRequest
{
    [Required(ErrorMessage = "O código é obrigatório.")]
    [StringLength(20, ErrorMessage = "Código muito longo.")]
    public string Code { get; set; } = string.Empty;
    
    public bool RememberMachine { get; set; } = false;
    
    public bool IsRecoveryCode { get; set; } = false;
}

/// <summary>
/// Request para desabilitar 2FA
/// </summary>
public class DisableTwoFactorRequest
{
    // Vazio - apenas confirmação via endpoint protegido
}

/// <summary>
/// Response genérica de sucesso
/// </summary>
public class TwoFactorSuccessResponse
{
    public string Message { get; set; } = string.Empty;
}
