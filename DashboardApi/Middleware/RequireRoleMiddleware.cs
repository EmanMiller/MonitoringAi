using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DashboardApi.Middleware;

/// <summary>
/// Enforces role checks. Use with Map().Use() for specific routes.
/// Logs permission denials with user ID, role, and attempted action.
/// </summary>
public class RequireRoleMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string[] _allowedRoles;
    private readonly ILogger<RequireRoleMiddleware> _logger;

    public RequireRoleMiddleware(RequestDelegate next, string[] allowedRoles, ILogger<RequireRoleMiddleware> logger)
    {
        _next = next;
        _allowedRoles = allowedRoles;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
        var path = context.Request.Path.Value ?? "";

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Permission denied: unauthenticated request to {Path}", path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized." });
            return;
        }

        var allowed = _allowedRoles.Length == 0 || _allowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase);
        if (!allowed)
        {
            _logger.LogWarning("Permission denied: UserId={UserId}, Role={Role}, Path={Path}", userId, role, path);
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { error = "Forbidden. You do not have permission for this action." });
            return;
        }

        await _next(context);
    }
}

public static class RequireRoleMiddlewareExtensions
{
    public static IApplicationBuilder UseRequireRole(this IApplicationBuilder app, params string[] roles)
    {
        return app.UseMiddleware<RequireRoleMiddleware>(roles);
    }
}
