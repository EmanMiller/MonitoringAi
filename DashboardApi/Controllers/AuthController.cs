using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DashboardApi.Data;
using DashboardApi.Services;

namespace DashboardApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    private const string AccessTokenCookieName = "access_token";
    private const string RefreshTokenCookieName = "refresh_token";
    private static readonly CookieOptions HttpOnlySecureCookie = new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/",
        IsEssential = true
    };

    private readonly AppDbContext _db;

    public AuthController(IAuthService authService, ILogger<AuthController> logger, AppDbContext db)
    {
        _authService = authService;
        _logger = logger;
        _db = db;
    }

    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request?.UserName))
            return BadRequest(new { error = "UserName is required." });
        var (valid, error) = PasswordValidator.Validate(request.Password);
        if (!valid) return BadRequest(new { error });
        if (await _db.Users.AnyAsync(u => u.UserName == request.UserName, ct))
            return BadRequest(new { error = "UserName already exists." });
        var user = new User
        {
            UserName = request.UserName!.Trim(),
            PasswordHash = PasswordValidator.HashPassword(request.Password!),
            Role = request.Role ?? "developer"
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("User registered: {UserName}", user.UserName);
        return Ok(new { message = "Registered. Please log in." });
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request?.UserName) || string.IsNullOrEmpty(request?.Password))
            return BadRequest(new { error = "UserName and Password are required." });

        var (success, user, error) = await _authService.ValidateLoginAsync(request.UserName!, request.Password!, ct);
        if (!success || user == null)
        {
            _logger.LogWarning("Login failed for user {UserName}: {Error}", request.UserName, error);
            return Unauthorized(new { error = error ?? "Invalid credentials." });
        }

        var (accessToken, refreshToken, refreshExpiry) = await _authService.IssueTokensAsync(user.Id, user.UserName, user.Role, ct);
        if (accessToken == null || refreshToken == null) return StatusCode(500, new { error = "Failed to issue tokens." });

        var accessOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            IsEssential = true,
            MaxAge = TimeSpan.FromHours(1)
        };
        Response.Cookies.Append(AccessTokenCookieName, accessToken, accessOptions);
        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            IsEssential = true,
            MaxAge = TimeSpan.FromDays(7)
        });

        _logger.LogInformation("User {UserId} ({UserName}) logged in successfully.", user.Id, user.UserName);
        return Ok(new { id = user.Id, userName = user.UserName, role = user.Role, expiresInMinutes = 60 });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        if (!Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshToken) || string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { error = "Refresh token missing." });

        var (valid, user) = await _authService.ValidateRefreshTokenAsync(refreshToken, ct);
        if (!valid || user == null)
        {
            Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions { Path = "/" });
            Response.Cookies.Delete(AccessTokenCookieName, new CookieOptions { Path = "/" });
            return Unauthorized(new { error = "Invalid or expired refresh token." });
        }

        var (accessToken, newRefreshToken, _) = await _authService.IssueTokensAsync(user.Id, user.UserName, user.Role, ct);
        if (accessToken == null) return StatusCode(500, new { error = "Failed to issue tokens." });

        Response.Cookies.Append(AccessTokenCookieName, accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            IsEssential = true,
            MaxAge = TimeSpan.FromHours(1)
        });
        if (!string.IsNullOrEmpty(newRefreshToken))
            Response.Cookies.Append(RefreshTokenCookieName, newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                IsEssential = true,
                MaxAge = TimeSpan.FromDays(7)
            });

        return Ok(new { id = user.Id, userName = user.UserName, role = user.Role, expiresInMinutes = 60 });
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshToken))
        {
            var (valid, user) = await _authService.ValidateRefreshTokenAsync(refreshToken, ct);
            if (valid && user != null)
                await _authService.RevokeRefreshTokenAsync(user.Id, ct);
        }
        Response.Cookies.Delete(AccessTokenCookieName, new CookieOptions { Path = "/" });
        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions { Path = "/" });
        return Ok(new { message = "Logged out." });
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        return Ok(new { id = userId, userName, role });
    }

    [HttpGet("csrf")]
    public IActionResult GetCsrfToken([FromServices] IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }
}

public class LoginRequest
{
    public string? UserName { get; set; }
    public string? Password { get; set; }
}

public class RegisterRequest
{
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string? Role { get; set; }
}
