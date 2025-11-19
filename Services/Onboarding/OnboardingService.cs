using erp.DTOs.Onboarding;
using erp.Data;
using erp.Models.Onboarding;
using Microsoft.EntityFrameworkCore;

namespace erp.Services.Onboarding;

public interface IOnboardingService
{
    List<OnboardingTour> GetAvailableTours(string userId, string[] userRoles);
    OnboardingTour? GetTourById(string tourId);
    Task<OnboardingProgress?> GetUserProgressAsync(string userId, string tourId);
    Task SaveProgressAsync(string userId, string tourId, int currentStep, bool isCompleted = false);
    Task CompleteStepAsync(string userId, string tourId, int stepIndex);
    Task CompleteTourAsync(string userId, string tourId);
    Task SkipTourAsync(string userId, string tourId);
    Task ResetTourAsync(string userId, string tourId);
    Task<bool> HasCompletedOnboardingAsync(string userId);
}

public class OnboardingService : IOnboardingService
{
    private readonly ApplicationDbContext _context;

    public OnboardingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<OnboardingTour> GetAvailableTours(string userId, string[] userRoles)
    {
        var tours = new List<OnboardingTour>
        {
            new OnboardingTour
            {
                Id = "getting-started",
                Name = "Bem-vindo ao Pillar ERP",
                Description = "Guia inicial para configurar sua conta e conhecer o sistema",
                IsRequired = true,
                Steps = new List<OnboardingStep>
                {
                    new OnboardingStep
                    {
                        Id = "welcome",
                        Title = "Bem-vindo ao Pillar",
                        Description = "Estamos felizes em tê-lo aqui! Este guia rápido vai te ajudar a configurar sua conta e entender os principais recursos do sistema.",
                        Target = "center",
                        Placement = "center",
                        Order = 1,
                        ShowPrevious = false
                    },
                    new OnboardingStep
                    {
                        Id = "security",
                        Title = "Segurança em Primeiro Lugar",
                        Description = "Sua segurança é nossa prioridade. Recomendamos ativar a Autenticação de Dois Fatores (2FA) e revisar suas configurações de senha.",
                        Target = "/settings", // Link to navigate
                        Placement = "center",
                        Order = 2
                    },
                    new OnboardingStep
                    {
                        Id = "customization",
                        Title = "Personalize sua Experiência",
                        Description = "Você pode ajustar o tema (Claro/Escuro), cores e layout do dashboard para trabalhar do seu jeito.",
                        Target = "/settings",
                        Placement = "center",
                        Order = 3
                    },
                    new OnboardingStep
                    {
                        Id = "modules",
                        Title = "Módulos Integrados",
                        Description = "O Pillar oferece módulos completos de Vendas, Estoque, Financeiro e RH. Tudo integrado para facilitar sua gestão.",
                        Target = "center",
                        Placement = "center",
                        Order = 4
                    },
                    new OnboardingStep
                    {
                        Id = "dashboard",
                        Title = "Seu Dashboard",
                        Description = "Acompanhe métricas em tempo real e tome decisões baseadas em dados. Você pode personalizar quais widgets aparecem aqui.",
                        Target = "/dashboard",
                        Placement = "center",
                        Order = 5
                    },
                    new OnboardingStep
                    {
                        Id = "finish",
                        Title = "Tudo Pronto!",
                        Description = "Você já conhece o básico. Explore o menu lateral para descobrir todas as funcionalidades. Bom trabalho!",
                        Target = "center",
                        Placement = "center",
                        Order = 6,
                        ShowNext = false
                    }
                }
            }
        };

        // Filtrar tours por role
        return tours.Where(t => 
            t.RequiredRoles == null || 
            t.RequiredRoles.Length == 0 || 
            t.RequiredRoles.Any(r => userRoles.Contains(r))
        ).ToList();
    }

    public OnboardingTour? GetTourById(string tourId)
    {
        // Por simplicidade, pegamos todos os tours e filtramos
        var allTours = GetAvailableTours("", Array.Empty<string>());
        return allTours.FirstOrDefault(t => t.Id == tourId);
    }

    public async Task<OnboardingProgress?> GetUserProgressAsync(string userId, string tourId)
    {
        if (!int.TryParse(userId, out int uid)) return null;

        var progress = await _context.UserOnboardingProgress
            .FirstOrDefaultAsync(p => p.UserId == uid && p.TourId == tourId);

        if (progress == null) return null;

        return new OnboardingProgress
        {
            UserId = userId,
            TourId = tourId,
            CurrentStep = progress.CurrentStep,
            IsCompleted = progress.IsCompleted,
            CompletedAt = progress.CompletedAt,
            StartedAt = progress.StartedAt
        };
    }

    public async Task SaveProgressAsync(string userId, string tourId, int currentStep, bool isCompleted = false)
    {
        if (!int.TryParse(userId, out int uid)) return;

        var progress = await _context.UserOnboardingProgress
            .FirstOrDefaultAsync(p => p.UserId == uid && p.TourId == tourId);

        if (progress == null)
        {
            progress = new UserOnboardingProgress
            {
                UserId = uid,
                TourId = tourId,
                CurrentStep = currentStep,
                IsCompleted = isCompleted,
                StartedAt = DateTime.UtcNow
            };
            _context.UserOnboardingProgress.Add(progress);
        }
        else
        {
            progress.CurrentStep = currentStep;
            progress.IsCompleted = isCompleted;
        }

        if (isCompleted && progress.CompletedAt == null)
        {
            progress.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public Task CompleteStepAsync(string userId, string tourId, int stepIndex)
    {
        return SaveProgressAsync(userId, tourId, stepIndex + 1);
    }

    public Task CompleteTourAsync(string userId, string tourId)
    {
        var tour = GetTourById(tourId);
        if (tour == null) return Task.CompletedTask;
        
        return SaveProgressAsync(userId, tourId, tour.Steps.Count, true);
    }

    public Task SkipTourAsync(string userId, string tourId)
    {
        return CompleteTourAsync(userId, tourId);
    }

    public async Task ResetTourAsync(string userId, string tourId)
    {
        if (!int.TryParse(userId, out int uid)) return;

        var progress = await _context.UserOnboardingProgress
            .FirstOrDefaultAsync(p => p.UserId == uid && p.TourId == tourId);

        if (progress != null)
        {
            _context.UserOnboardingProgress.Remove(progress);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> HasCompletedOnboardingAsync(string userId)
    {
        if (!int.TryParse(userId, out int uid)) return false;

        return await _context.UserOnboardingProgress
            .AnyAsync(p => p.UserId == uid && p.TourId == "getting-started" && p.IsCompleted);
    }
}
