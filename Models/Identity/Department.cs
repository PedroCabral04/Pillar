using System.ComponentModel.DataAnnotations;
using erp.Models.Audit;

namespace erp.Models.Identity;

public class Department : IAuditable
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(10)]
    public string? Code { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Relacionamento hierárquico (departamento pai)
    public int? ParentDepartmentId { get; set; }
    public Department? ParentDepartment { get; set; }
    
    // Gerente do departamento
    public int? ManagerId { get; set; }
    public ApplicationUser? Manager { get; set; }
    
    // Centro de custo
    [MaxLength(50)]
    public string? CostCenter { get; set; }
    
    // Metadados
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navegação
    public ICollection<ApplicationUser> Employees { get; set; } = new List<ApplicationUser>();
    public ICollection<Department> SubDepartments { get; set; } = new List<Department>();
}
