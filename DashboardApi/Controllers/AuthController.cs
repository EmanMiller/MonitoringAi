using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DashboardApi.Data;
using DashboardApi.Services;

namespace DashboardApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const string AccessTokenCookieName = "access_token";
    private const string RefreshTokenCookieName = "refresh_token";

    private readonly ApplicationDbContext _db;
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    private readonly IHostEnvironment _env;

    public AuthController(ILogger<AuthController> logger, ApplicationDbContext db, IAuthService authService, IHostEnvironment env)
    {
        _logger = logger;
        _db = db;
        _authService = authService;
        _env = env;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request?.UserName) || string.IsNullOrEmpty(request?.Password))
            return BadRequest(new { error = "UserName and Password are required." });

        var userName = (request.UserName ?? "user").Trim();
        UserInfo info;

        var existingUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == userName, ct);
        if (existingUser != null)
        {
            var (success, userInfo, error) = await _authService.ValidateLoginAsync(userName, request.Password!, ct);
            if (!success)
            {
                _logger.LogWarning("Login failed for {UserName}: {Error}", userName, error);
                return Unauthorized(new { error = error ?? "Invalid credentials." });
            }
            info = userInfo!;
        }
        else
        {
            var newUser = new User
            {
                Id = Guid.NewGuid(),
                Username = userName,
                Email = $"{userName}@local",
                PasswordHash = PasswordValidator.HashPassword(request.Password!),
                Role = "admin",
                CreatedAt = DateTime.UtcNow
            };
            _db.Users.Add(newUser);
            await _db.SaveChangesAsync(ct);
            info = new UserInfo { Id = newUser.Id, UserName = newUser.Username, Role = newUser.Role };
        }

        var (accessToken, refreshToken, _) = await _authService.IssueTokensAsync(info.Id, info.UserName, info.Role, ct);
        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
        {
            _logger.LogError("Failed to issue tokens for {UserName}", userName);
            return StatusCode(500, new { error = "Failed to issue tokens." });
        }

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !_env.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Path = "/"
        };
        Response.Cookies.Append(AccessTokenCookieName, accessToken, cookieOptions);
        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, cookieOptions);

        _logger.LogInformation("Login successful for {UserName}.", userName);
        return Ok(new { id = info.Id.ToString(), userName = info.UserName, role = info.Role, expiresInMinutes = 60 });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AccessTokenCookieName, new CookieOptions { Path = "/" });
        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions { Path = "/" });
        return Ok(new { message = "Logged out." });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
            return Unauthorized();

        return Ok(new { id = user.Id.ToString(), userName = user.Username, role = user.Role });
    }

    /// <summary>Barebones: return a dummy token so frontend doesn't break if it still calls this.</summary>
    [HttpGet("csrf")]
    public IActionResult GetCsrfToken()
    {
        return Ok(new { token = "dummy-csrf-barebones" });
    }
}

public class LoginRequest
{
    public string? UserName { get; set; }
    public string? Password { get; set; }
}
