using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DashboardApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DashboardApi.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly JwtSettings _jwt;
    private readonly ILogger<AuthService> _logger;
    private const int LoginLockoutMinutes = 15;
    private const int MaxFailedAttempts = 5;

    public AuthService(AppDbContext db, IOptions<JwtSettings> jwt, ILogger<AuthService> logger)
    {
        _db = db;
        _jwt = jwt.Value;
        _logger = logger;
    }

    public async Task<(bool Success, UserInfo? User, string? Error)> ValidateLoginAsync(string userName, string password, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserName == userName, ct);
        if (user == null)
            return (false, null, "Invalid credentials.");

        if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > DateTime.UtcNow)
            return (false, null, $"Account locked. Try again after {user.LockoutEndUtc:g} UTC.");

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            await RecordFailedLoginInternalAsync(user.Id, ct);
            return (false, null, "Invalid credentials.");
        }

        await RecordSuccessfulLoginInternalAsync(user.Id, ct);
        return (true, new UserInfo { Id = user.Id, UserName = user.UserName, Role = user.Role }, null);
    }

    public async Task<(string? AccessToken, string? RefreshToken, DateTime? RefreshExpiry)> IssueTokensAsync(int userId, string userName, string role, CancellationToken ct = default)
    {
        var accessToken = GenerateAccessToken(userId, userName, role);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshExpiry = DateTime.UtcNow.AddDays(7);

        var user = await _db.Users.FindAsync(new object[] { userId }, ct);
        if (user != null)
        {
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryUtc = refreshExpiry;
            await _db.SaveChangesAsync(ct);
        }

        return (accessToken, refreshToken, refreshExpiry);
    }

    public async Task<(bool Valid, UserInfo? User)> ValidateRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken, ct);
        if (user == null || user.RefreshTokenExpiryUtc == null || user.RefreshTokenExpiryUtc < DateTime.UtcNow)
            return (false, null);
        return (true, new UserInfo { Id = user.Id, UserName = user.UserName, Role = user.Role });
    }

    public async Task RevokeRefreshTokenAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, ct);
        if (user != null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryUtc = null;
            await _db.SaveChangesAsync(ct);
        }
    }

    public void RecordFailedLogin(int userId) => _ = RecordFailedLoginInternalAsync(userId, default);
    public void RecordSuccessfulLogin(int userId) => _ = RecordSuccessfulLoginInternalAsync(userId, default);

    private async Task RecordFailedLoginInternalAsync(int userId, CancellationToken ct)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, ct);
        if (user == null) return;
        user.FailedLoginAttempts++;
        if (user.FailedLoginAttempts >= MaxFailedAttempts)
            user.LockoutEndUtc = DateTime.UtcNow.AddMinutes(LoginLockoutMinutes);
        await _db.SaveChangesAsync(ct);
    }

    private async Task RecordSuccessfulLoginInternalAsync(int userId, CancellationToken ct)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, ct);
        if (user == null) return;
        user.FailedLoginAttempts = 0;
        user.LockoutEndUtc = null;
        await _db.SaveChangesAsync(ct);
    }

    private string GenerateAccessToken(int userId, string userName, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class JwtSettings
{
    public const string SectionName = "Jwt";
    public string Secret { get; set; } = "";
    public string Issuer { get; set; } = "SumoLogic.DashboardApi";
    public string Audience { get; set; } = "SumoLogic.DashboardFrontend";
    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 7;
}
