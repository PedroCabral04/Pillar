using System;
using System.Threading.Tasks;

namespace erp.Services.Onboarding;

public interface IOnboardingResumeService
{
    void SetPendingResume(string userId, string tourId);
    void ClearPendingResume();
    bool HasPending { get; }
    (string userId, string tourId)? GetPending();
    event Action? Changed;
}

public class OnboardingResumeService : IOnboardingResumeService
{
    private string? _userId;
    private string? _tourId;

    public event Action? Changed;

    public bool HasPending => !string.IsNullOrEmpty(_userId) && !string.IsNullOrEmpty(_tourId);

    public void SetPendingResume(string userId, string tourId)
    {
        _userId = userId;
        _tourId = tourId;
        Changed?.Invoke();
    }

    public void ClearPendingResume()
    {
        _userId = null;
        _tourId = null;
        Changed?.Invoke();
    }

    public (string userId, string tourId)? GetPending()
    {
        if (!HasPending) return null;
        return (_userId!, _tourId!);
    }
}
