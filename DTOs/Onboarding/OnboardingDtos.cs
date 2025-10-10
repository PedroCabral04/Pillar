namespace erp.DTOs.Onboarding;

public class OnboardingStep
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string Target { get; set; } // CSS selector
    public string? Placement { get; set; } = "bottom"; // top, bottom, left, right
    public int Order { get; set; }
    public bool ShowNext { get; set; } = true;
    public bool ShowPrevious { get; set; } = true;
    public bool ShowSkip { get; set; } = true;
}

public class OnboardingTour
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required List<OnboardingStep> Steps { get; set; }
    public bool IsRequired { get; set; }
    public string[]? RequiredRoles { get; set; }
}

public class OnboardingProgress
{
    public required string TourId { get; set; }
    public required string UserId { get; set; }
    public bool IsCompleted { get; set; }
    public int CurrentStep { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
}
