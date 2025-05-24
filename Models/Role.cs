using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace erp.Models;

public class Role {

    [Key]
    public int Id { get; init; }
    public required string Name { get; set; }
    public virtual required ICollection<User> Users { get; set; }
}
