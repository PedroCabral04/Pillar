using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using erp.Models.Identity;

namespace erp.Models.Kanban;

public class KanbanBoard
{
    public int Id { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = "Meu quadro";

    // owner is an ApplicationUser.Id
    public int OwnerId { get; set; }
    public ApplicationUser Owner { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<KanbanColumn> Columns { get; set; } = new();
    public List<KanbanLabel> Labels { get; set; } = new();
}
