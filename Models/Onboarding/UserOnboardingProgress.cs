using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using erp.Models.Identity;

namespace erp.Models.Onboarding;

[Table("UserOnboardingProgress")]
public class UserOnboardingProgress
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual ApplicationUser User { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string TourId { get; set; } = string.Empty;

    public int CurrentStep { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }
}
