using erp.DTOs.Onboarding;

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
}

public class OnboardingService : IOnboardingService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, OnboardingProgress> _progressCache = new();

    public OnboardingService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public List<OnboardingTour> GetAvailableTours(string userId, string[] userRoles)
    {
        var tours = new List<OnboardingTour>
        {
            new OnboardingTour
            {
                Id = "welcome-tour",
                Name = "Bem-vindo ao Pillar ERP",
                Description = "Conheça as principais funcionalidades do sistema",
                IsRequired = true,
                Steps = new List<OnboardingStep>
                {
                    new OnboardingStep
                    {
                        Id = "step-1",
                        Title = "Bem-vindo!",
                        Description = "Este é o Pillar ERP, sua solução completa para gestão empresarial. Vamos fazer um tour rápido!",
                        Target = "#main-content",
                        Placement = "center",
                        Order = 1,
                        ShowPrevious = false
                    },
                    new OnboardingStep
                    {
                        Id = "step-2",
                        Title = "Dashboard",
                        Description = "Aqui você encontra uma visão geral do seu negócio com métricas e gráficos em tempo real.",
                        Target = ".dashboard-section",
                        Placement = "bottom",
                        Order = 2
                    },
                    new OnboardingStep
                    {
                        Id = "step-3",
                        Title = "Menu de Navegação",
                        Description = "Use este menu para navegar entre as diferentes seções do sistema.",
                        Target = ".mud-drawer",
                        Placement = "right",
                        Order = 3
                    },
                    new OnboardingStep
                    {
                        Id = "step-4",
                        Title = "Perfil do Usuário",
                        Description = "Acesse suas configurações, preferências e faça logout por aqui.",
                        Target = ".mud-appbar .mud-menu",
                        Placement = "bottom",
                        Order = 4
                    },
                    new OnboardingStep
                    {
                        Id = "step-5",
                        Title = "Notificações",
                        Description = "Fique por dentro de todas as atualizações importantes através das notificações.",
                        Target = ".notification-icon",
                        Placement = "bottom",
                        Order = 5
                    },
                    new OnboardingStep
                    {
                        Id = "step-6",
                        Title = "Pronto!",
                        Description = "Você está pronto para começar. Explore o sistema e descubra tudo o que ele pode fazer!",
                        Target = "#main-content",
                        Placement = "center",
                        Order = 6,
                        ShowNext = false
                    }
                }
            },
            new OnboardingTour
            {
                Id = "dashboard-tour",
                Name = "Tour do Dashboard",
                Description = "Descubra como usar o dashboard para visualizar métricas e personalizar sua experiência",
                IsRequired = false,
                Steps = new List<OnboardingStep>
                {
                    new OnboardingStep
                    {
                        Id = "dashboard-1",
                        Title = "Bem-vindo ao Dashboard!",
                        Description = "Aqui você encontra uma visão completa do seu negócio com gráficos interativos e métricas em tempo real.",
                        Target = ".dashboard-section",
                        Placement = "center",
                        Order = 1,
                        ShowPrevious = false
                    },
                    new OnboardingStep
                    {
                        Id = "dashboard-2",
                        Title = "Saudação Personalizada",
                        Description = "O dashboard te cumprimenta de acordo com o horário do dia e exibe a data atual.",
                        Target = ".mud-paper:first-child",
                        Placement = "bottom",
                        Order = 2
                    },
                    new OnboardingStep
                    {
                        Id = "dashboard-3",
                        Title = "Filtros de Data",
                        Description = "Use este seletor para escolher um período personalizado e visualizar dados específicos.",
                        Target = ".mud-picker",
                        Placement = "bottom",
                        Order = 3
                    },
                    new OnboardingStep
                    {
                        Id = "dashboard-4",
                        Title = "Presets Rápidos",
                        Description = "Economize tempo com presets: hoje, últimos 7 dias, este mês e mais!",
                        Target = ".mud-chipset",
                        Placement = "bottom",
                        Order = 4
                    },
                    new OnboardingStep
                    {
                        Id = "dashboard-5",
                        Title = "Widgets Interativos",
                        Description = "Cada widget mostra métricas importantes. Clique neles para ver mais detalhes!",
                        Target = ".mud-grid",
                        Placement = "top",
                        Order = 5
                    },
                    new OnboardingStep
                    {
                        Id = "dashboard-6",
                        Title = "Você está pronto!",
                        Description = "Explore os widgets, experimente os filtros e personalize seu dashboard. Bom trabalho!",
                        Target = ".dashboard-section",
                        Placement = "center",
                        Order = 6,
                        ShowNext = false
                    }
                }
            },
            new OnboardingTour
            {
                Id = "admin-tour",
                Name = "Tour de Administração",
                Description = "Aprenda a gerenciar usuários e configurações do sistema",
                IsRequired = false,
                RequiredRoles = new[] { "Admin" },
                Steps = new List<OnboardingStep>
                {
                    new OnboardingStep
                    {
                        Id = "admin-1",
                        Title = "Painel de Administração",
                        Description = "Esta é a área administrativa onde você gerencia todo o sistema.",
                        Target = ".admin-section",
                        Placement = "bottom",
                        Order = 1,
                        ShowPrevious = false
                    },
                    new OnboardingStep
                    {
                        Id = "admin-2",
                        Title = "Gestão de Usuários",
                        Description = "Crie, edite e gerencie usuários do sistema, suas permissões e roles.",
                        Target = ".users-table",
                        Placement = "top",
                        Order = 2
                    },
                    new OnboardingStep
                    {
                        Id = "admin-3",
                        Title = "Configurações do Sistema",
                        Description = "Ajuste configurações globais, integrações e personalizações.",
                        Target = ".settings-link",
                        Placement = "right",
                        Order = 3,
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

    public Task<OnboardingProgress?> GetUserProgressAsync(string userId, string tourId)
    {
        var key = $"{userId}_{tourId}";
        _progressCache.TryGetValue(key, out var progress);
        return Task.FromResult(progress);
    }

    public Task SaveProgressAsync(string userId, string tourId, int currentStep, bool isCompleted = false)
    {
        var key = $"{userId}_{tourId}";
        
        if (!_progressCache.TryGetValue(key, out var progress))
        {
            progress = new OnboardingProgress
            {
                UserId = userId,
                TourId = tourId,
                CurrentStep = currentStep,
                IsCompleted = isCompleted
            };
        }
        else
        {
            progress.CurrentStep = currentStep;
            progress.IsCompleted = isCompleted;
        }

        if (isCompleted)
        {
            progress.CompletedAt = DateTime.UtcNow;
        }

        _progressCache[key] = progress;
        return Task.CompletedTask;
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

    public Task ResetTourAsync(string userId, string tourId)
    {
        var key = $"{userId}_{tourId}";
        _progressCache.Remove(key);
        return Task.CompletedTask;
    }
}
