using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace erp.Models.Kanban;

public class KanbanBoard
{
    public int Id { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = "Meu quadro";

    // owner is an ApplicationUser.Id
    public int OwnerId { get; set; }

    public List<KanbanColumn> Columns { get; set; } = new();
}
