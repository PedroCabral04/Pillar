namespace erp.DTOs.User;

public class UpdateUserDto
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string? Password { get; set; }
    public List<int> RoleIds { get; set; } = new();
    public bool IsActive { get; set; }
}
