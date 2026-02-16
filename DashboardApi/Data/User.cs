namespace DashboardApi.Data;

/// <summary>User: Id (Guid), Username, Email, PasswordHash, CreatedAt. Role/RefreshToken for auth.</summary>
public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public string Role { get; set; } = "developer";
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryUtc { get; set; }
    public DateTime? LockoutEndUtc { get; set; }
    public int FailedLoginAttempts { get; set; }
}
