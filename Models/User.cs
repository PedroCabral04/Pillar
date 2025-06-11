using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace erp.Models;

/// <summary>
/// Modelo que representa um usuário no sistema
/// </summary>
[Index(nameof(Email), IsUnique = true)]
public class User
{
    
    [Key]
    public int Id { get; init; }
    
    [Required(ErrorMessage = "Nome de usuário é obrigatório")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Nome de usuário deve ter entre 3 e 50 caracteres")]
    public string Username { get; set; }
    
    [Required(ErrorMessage = "E-mail é obrigatório")]
    [EmailAddress(ErrorMessage = "Formato de e-mail inválido")]
    [StringLength(100, ErrorMessage = "E-mail não pode exceder 100 caracteres")]
    public string Email { get; set; }
    
    [Required]
    [Phone]
    [StringLength(11, ErrorMessage = "Telefone inválido")]
    public string Phone { get; set; }
    
    [Required]
    [JsonIgnore] 
    [Column(TypeName = "varchar(255)")]
    [Comment("Hash BCrypt da senha do usuário")]
    public string PasswordHash { get; set; }
    
    [JsonIgnore]
    public DateTime? PasswordChangedAt { get; set; }
    
    [JsonIgnore]
    [Column(TypeName = "smallint")]
    [Comment("Número de tentativas de login mal-sucedidas")]
    public int? FailedLoginAttempts { get; set; } = 0;
    
    [JsonIgnore]
    public DateTime? LockedUntil { get; set; }
    
    [NotMapped]
    [JsonIgnore]
    public bool IsLocked => 
        LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;
    
    [NotMapped]
    [JsonIgnore]
    public bool PasswordRequiresReset => 
        PasswordChangedAt.HasValue && (DateTime.UtcNow - PasswordChangedAt.Value).TotalDays > 90;
    
    [Required]
    public List<int> RoleIds { get; set; } = new();
    
    [NotMapped]
    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
    
    public bool IsActive { get; set; } = true;
    
    // Campos adicionais para auditoria e segurança
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastUpdatedAt { get; set; }
    
    [JsonIgnore]
    public DateTime? LastLoginAt { get; set; }
    
    
    public User(int id, string username, string email, string phone, string passwordHash, List<int> roleIds, bool isActive)
    {
        Id = id;
        Username = username;
        Email = email;
        Phone = phone;
        PasswordHash = passwordHash;
        RoleIds = roleIds;
        IsActive = isActive;
        CreatedAt = DateTime.UtcNow;
    }
    
    
    
    
    // Construtor sem parâmetros para Entity Framework
    public User()
    {
        
    }
}
