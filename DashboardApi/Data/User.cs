namespace DashboardApi.Data;

public class User
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "developer"; // developer | senior_developer | admin
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryUtc { get; set; }
    public DateTime? LockoutEndUtc { get; set; }
    public int FailedLoginAttempts { get; set; }
}
