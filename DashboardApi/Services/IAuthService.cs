namespace DashboardApi.Services;

public interface IAuthService
{
    Task<(bool Success, UserInfo? User, string? Error)> ValidateLoginAsync(string userName, string password, CancellationToken ct = default);
    Task<(string? AccessToken, string? RefreshToken, DateTime? RefreshExpiry)> IssueTokensAsync(Guid userId, string userName, string role, CancellationToken ct = default);
    Task<(bool Valid, UserInfo? User)> ValidateRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task RevokeRefreshTokenAsync(Guid userId, CancellationToken ct = default);
    void RecordFailedLogin(Guid userId);
    void RecordSuccessfulLogin(Guid userId);
}

public class UserInfo
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = "";
    public string Role { get; set; } = "";
}
