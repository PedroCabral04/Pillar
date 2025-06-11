namespace erp.DTOs.User;

using System.ComponentModel.DataAnnotations.Schema;

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    
    [NotMapped]
    public List<string> RoleNames { get; set; } = new();
    
    public bool IsActive { get; set; }
}
