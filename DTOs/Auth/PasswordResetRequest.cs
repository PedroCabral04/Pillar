using System.ComponentModel.DataAnnotations;

namespace erp.DTOs.Auth;

/// <summary>
/// Request para solicitar recuperação de senha
/// </summary>
public class PasswordResetRequest
{
    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Request para confirmar redefinição de senha
/// </summary>
public class PasswordResetConfirmRequest
{
    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Token é obrigatório")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nova senha é obrigatória")]
    [MinLength(8, ErrorMessage = "A senha deve ter no mínimo 8 caracteres")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirmação de senha é obrigatória")]
    [Compare(nameof(NewPassword), ErrorMessage = "As senhas não coincidem")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Request para alterar senha do usuário autenticado
/// </summary>
public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Senha atual é obrigatória")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nova senha é obrigatória")]
    [MinLength(8, ErrorMessage = "A senha deve ter no mínimo 8 caracteres")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirmação de senha é obrigatória")]
    [Compare(nameof(NewPassword), ErrorMessage = "As senhas não coincidem")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
