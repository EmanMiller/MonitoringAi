using System.Collections.Concurrent;

namespace DashboardApi.Middleware;

/// <summary>
/// Rate limit login attempts: max 5 per 15 minutes per key (IP or username), then lockout.
/// </summary>
public class LoginRateLimitMiddleware
{
    private static readonly ConcurrentDictionary<string, LoginAttemptRecord> Attempts = new();
    private const int MaxAttempts = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
    private readonly RequestDelegate _next;
    private readonly ILogger<LoginRateLimitMiddleware> _logger;

    public LoginRateLimitMiddleware(RequestDelegate next, ILogger<LoginRateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/auth/login", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var now = DateTime.UtcNow;
        var record = Attempts.AddOrUpdate(key,
            _ => new LoginAttemptRecord(1, now),
            (_, r) =>
            {
                if (now - r.FirstAttempt > Window)
                    return new LoginAttemptRecord(1, now);
                return new LoginAttemptRecord(r.Count + 1, r.FirstAttempt);
            });

        if (record.Count > MaxAttempts)
        {
            var retryAfter = record.FirstAttempt.Add(Window) - now;
            _logger.LogWarning("Login rate limit exceeded for key {Key}. Blocked until {Until}", key, record.FirstAttempt.Add(Window));
            context.Response.StatusCode = 429;
            context.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
            await context.Response.WriteAsJsonAsync(new { error = "Too many login attempts. Try again in 15 minutes." });
            return;
        }

        await _next(context);
    }

    private class LoginAttemptRecord
    {
        public int Count { get; set; }
        public DateTime FirstAttempt { get; set; }
        public LoginAttemptRecord(int count, DateTime first) { Count = count; FirstAttempt = first; }
    }
}
