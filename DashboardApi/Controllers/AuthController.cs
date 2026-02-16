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
    private readonly ILogger<AuthController> _logger;

    public AuthController(ILogger<AuthController> logger, ApplicationDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    /// <summary>Barebones: accept any non-empty username/password and return success.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request?.UserName) || string.IsNullOrEmpty(request?.Password))
            return BadRequest(new { error = "UserName and Password are required." });

        var userName = (request.UserName ?? "user").Trim();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == userName, ct);
        if (user == null)
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
            user = newUser;
        }

        _logger.LogInformation("Login (barebones) for {UserName}.", userName);
        return Ok(new { id = user.Id.ToString(), userName = user.Username, role = user.Role, expiresInMinutes = 60 });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AccessTokenCookieName, new CookieOptions { Path = "/" });
        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions { Path = "/" });
        return Ok(new { message = "Logged out." });
    }

    /// <summary>Barebones: return a default user so the app can load without real auth.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var first = await _db.Users.AsNoTracking().FirstOrDefaultAsync(ct);
        if (first != null)
            return Ok(new { id = first.Id.ToString(), userName = first.Username, role = first.Role });
        return Unauthorized();
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
