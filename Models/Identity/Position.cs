using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using erp.Models.Audit;
using erp.Models;

namespace erp.Models.Identity;

public class Position : IAuditable, IMustHaveTenant
{
    public int Id { get; set; }

    public int TenantId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(10)]
    public string? Code { get; set; }
    
    // Nível hierárquico (1 = Estagiário, 5 = Diretor, etc)
    public int? Level { get; set; }
    
    // Faixa salarial
    [Column(TypeName = "decimal(18,2)")]
    public decimal? MinSalary { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal? MaxSalary { get; set; }
    
    // Departamento padrão para este cargo
    public int? DefaultDepartmentId { get; set; }
    public Department? DefaultDepartment { get; set; }
    
    // Requisitos e responsabilidades
    public string? Requirements { get; set; }
    public string? Responsibilities { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Metadados
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navegação
    public ICollection<ApplicationUser> Employees { get; set; } = new List<ApplicationUser>();
}
