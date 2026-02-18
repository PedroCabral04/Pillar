namespace erp.DTOs.User;

/// <summary>
/// DTO para atualização de roles de um usuário
/// </summary>
public class UpdateUserRolesDto
{
    /// <summary>
    /// Lista de IDs das roles a serem atribuídas ao usuário
    /// </summary>
    public required List<int> RoleIds { get; set; }
}
