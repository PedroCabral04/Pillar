namespace erp.DTOs.User;

using System.ComponentModel.DataAnnotations.Schema;

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public int RoleId { get; set; }
    
    [NotMapped]
    public string RoleName { get; set; }
    
    public bool IsActive { get; set; }
}
