namespace erp.DTOs.User;

using System.ComponentModel.DataAnnotations.Schema;

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    
    [NotMapped]
    public List<string> RoleNames { get; set; } = new();
    
    [NotMapped]
    public List<string> RoleAbbreviations { get; set; } = new();
    
    public bool IsActive { get; set; }
    
    // Informações Pessoais
    public string? FullName { get; set; }
    public string? Cpf { get; set; }
    public string? Rg { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? MaritalStatus { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    
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
    public string? DepartmentName { get; set; }
    public int? PositionId { get; set; }
    public string? PositionTitle { get; set; }
    public decimal? Salary { get; set; }
    public DateTime? HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
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
    
    // Metadados
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
