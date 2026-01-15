using System.ComponentModel.DataAnnotations;

using erp.DTOs.Role;
using erp.Validation;

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
    
    // Informações Pessoais
    public string? FullName { get; set; }

    [Cpf(ErrorMessage = "CPF inválido.")]
    public string? Cpf { get; set; }

    public string? Rg { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? MaritalStatus { get; set; }
    
    // Endereço
    public string? PostalCode { get; set; }
    public string? Street { get; set; }
    public string? Number { get; set; }
    public string? Complement { get; set; }
    public string? Neighborhood { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    
    // Informações Profissionais
    public int? DepartmentId { get; set; }
    public int? PositionId { get; set; }
    public decimal? Salary { get; set; }
    public DateTime? HireDate { get; set; }
    public string? ContractType { get; set; }
    public string? EmploymentStatus { get; set; }
    
    // Informações Bancárias
    public string? BankCode { get; set; }
    public string? BankName { get; set; }
    public string? BankAgency { get; set; }
    public string? BankAccount { get; set; }
    public string? BankAccountType { get; set; }
    
    // Informações de Emergência
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public string? EmergencyContactPhone { get; set; }
    
    // Documentos
    public string? WorkCard { get; set; }
    public string? PisNumber { get; set; }
    public string? EducationLevel { get; set; }
}