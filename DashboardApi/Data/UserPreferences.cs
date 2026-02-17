namespace DashboardApi.Data;

/// <summary>
/// Onboarding preferences per user. UserId can be Guid string or anonymous id.
/// </summary>
public class UserPreferences
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public bool OnboardingCompleted { get; set; }
    public bool OnboardingSkipped { get; set; }
    public int LastOnboardingStep { get; set; }
    public string SelectedInterestsJson { get; set; } = "[]"; // JSON array of strings
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
