using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace DashboardApi.Middleware;

/// <summary>
/// Reads JWT from httpOnly cookie "access_token" and sets Authorization header so JWT Bearer auth works.
/// </summary>
public class JwtCookieMiddleware
{
    private readonly RequestDelegate _next;

    public JwtCookieMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Cookies.TryGetValue("access_token", out var token) && !string.IsNullOrEmpty(token))
        {
            if (!context.Request.Headers.ContainsKey("Authorization"))
                context.Request.Headers.Append("Authorization", $"{JwtBearerDefaults.AuthenticationScheme} {token}");
        }
        await _next(context);
    }
}
