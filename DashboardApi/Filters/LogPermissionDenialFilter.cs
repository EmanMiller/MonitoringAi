using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DashboardApi.Filters;

/// <summary>
/// Logs all permission denials (403) with user ID, role, and attempted action.
/// </summary>
public class LogPermissionDenialFilter : IResultFilter
{
    private readonly ILogger<LogPermissionDenialFilter> _logger;

    public LogPermissionDenialFilter(ILogger<LogPermissionDenialFilter> logger) => _logger = logger;

    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is not ForbidResult) return;
        var userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = context.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value;
        var path = context.HttpContext.Request.Path.Value ?? "";
        var method = context.HttpContext.Request.Method;
        _logger.LogWarning("Permission denied: UserId={UserId}, Role={Role}, Action={Method} {Path}", userId ?? "anonymous", role ?? "none", method, path);
    }

    public void OnResultExecuted(ResultExecutedContext context) { }
}
