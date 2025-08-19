using System.ComponentModel.DataAnnotations;

using erp.DTOs.Role;

namespace erp.DTOs.User;

public class CreateUserDto
{
    [Required(ErrorMessage = "Nome de usuário é obrigatório")]
    public string Username { get; set; } = string.Empty;
    
    // Senha não é mais obrigatória no DTO pois será gerada automaticamente
    public string? Password { get; set; }
    
    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Telefone é obrigatório")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role é obrigatória")]
    [MinLength(1, ErrorMessage = "Escolha pelo menos uma função/permissão")]
    public required List<int> RoleIds { get; set; } = new List<int>();
}