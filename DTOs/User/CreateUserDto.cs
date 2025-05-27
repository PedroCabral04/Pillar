using System.ComponentModel.DataAnnotations;

namespace erp.DTOs.User;

public class CreateUserDto
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public int RoleId { get; set; }
}
