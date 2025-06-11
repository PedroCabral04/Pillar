using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace erp.Models;

public class Role {

    [Key]
    public int Id { get; init; }
    public required string Name { get; set; }

    [Required(ErrorMessage = "Abreviação é obrigatória")]
    [StringLength(10, MinimumLength = 2, ErrorMessage = "A abreviação deve ter entre 2 e 10 caracteres")]
    public required string Abbreviation { get; set; } = string.Empty;

    public virtual required ICollection<User> Users { get; set; }
}
