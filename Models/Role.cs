using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace erp.Models;

/// <summary>
/// Role model for user authorization.
/// </summary>
/// <remarks>
/// OBSOLETE: Use <see cref="Models.Identity.ApplicationRole"/> instead.
/// This legacy Role model is kept for backwards compatibility only.
/// New code should use ApplicationRole from Models.Identity which is integrated with ASP.NET Core Identity.
/// </remarks>
[Obsolete("Use ApplicationRole from Models.Identity instead. This legacy model will be removed in future versions.")]
public class Role {

    [Key]
    public int Id { get; init; }
    public required string Name { get; set; }

    [Required(ErrorMessage = "Abreviação é obrigatória")]
    [StringLength(20, MinimumLength = 2, ErrorMessage = "A abreviação deve ter entre 2 e 10 caracteres")]
    public required string Abbreviation { get; set; } = string.Empty;

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
