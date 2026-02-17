using System.Text.RegularExpressions;
using DashboardApi.Models;

namespace DashboardApi.Services;

/// <summary>
/// Centralized input validation and sanitization for security.
/// </summary>
public static class InputValidationService
{
    // Dashboard name: must start with uppercase letter, then alphanumeric/spaces/_- only, 3-50 chars
    private static readonly Regex DashboardNameRegex = new(@"^[A-Z][a-zA-Z0-9\s_-]{2,50}$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
    private static readonly string[] DangerousSqlKeywords = { "drop", "delete", "truncate", "insert", "update", "exec", "execute", "--", "/*", "*/", "xp_", "sp_", ";", "\"", "'" };
    private static readonly string[] DangerousScriptTags = { "<script", "</script", "<iframe", "<object", "<embed", "javascript:" };
    private static readonly char[] CharsToEscape = { '<', '>', '"', '\'', ';' };
    private const int DashboardNameMaxLength = 50;
    private const int QueryKeyMaxLength = 200;
    private const int ChatMessageMaxLength = 2000;
    private const int SearchQueryMaxLength = 100;
    private const int MatchQueryUserInputMaxLength = 500;

    public static (bool Valid, string? Error) ValidateDashboardName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return (false, "Dashboard name is required.");
        var stripped = StripHtmlAndTrim(name);
        if (stripped.Length > DashboardNameMaxLength) return (false, $"Dashboard name must be at most {DashboardNameMaxLength} characters.");
        if (ContainsDangerousSql(stripped)) return (false, "Dashboard name contains invalid content.");
        if (ContainsScriptTags(stripped)) return (false, "Dashboard name contains invalid content.");
        if (!DashboardNameRegex.IsMatch(stripped)) return (false, "Dashboard name must start with an uppercase letter and contain only letters, numbers, spaces, hyphens, or underscores (3-50 characters).");
        return (true, null);
    }

    public static string SanitizeDashboardName(string? input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        var s = StripHtmlAndTrim(input);
        foreach (var c in CharsToEscape)
            s = s.Replace(c.ToString(), "");
        s = Regex.Replace(s, @"\s+", " ").Trim();
        if (s.Length > DashboardNameMaxLength) s = s[..DashboardNameMaxLength];
        return s;
    }

    public static (bool Valid, string? Error) ValidateQueryLibraryKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return (false, "Key is required.");
        var sanitized = SanitizeHtmlEntities(key);
        if (sanitized.Length > QueryKeyMaxLength) return (false, $"Key must be at most {QueryKeyMaxLength} characters.");
        return (true, null);
    }

    public static (bool Valid, string? Error) ValidateQueryLibraryValue(string? value)
    {
        if (value == null) value = "";
        if (ContainsCommandInjection(value)) return (false, "Query value contains disallowed characters or commands.");
        return (true, null);
    }

    public static (bool Valid, string? Error) ValidateChatMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return (false, "Message is required.");
        if (message.Length > ChatMessageMaxLength) return (false, $"Message must be at most {ChatMessageMaxLength} characters.");
        if (ContainsScriptTags(message)) return (false, "Message contains invalid content.");
        return (true, null);
    }

    public static string SanitizeChatMessage(string? input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        var s = input;
        foreach (var tag in DangerousScriptTags)
            s = Regex.Replace(s, tag, "", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
        s = Regex.Replace(s, @"<[^>]*>", "", RegexOptions.None, TimeSpan.FromMilliseconds(100));
        if (s.Length > ChatMessageMaxLength) s = s[..ChatMessageMaxLength];
        return s.Trim();
    }

    public static (bool Valid, string? Error) ValidateSearchQuery(string? q)
    {
        if (q == null) return (true, null);
        if (q.Length > SearchQueryMaxLength) return (false, $"Search query must be at most {SearchQueryMaxLength} characters.");
        return (true, null);
    }

    /// <summary>Validate user input for match-query endpoint (max 500 chars, no script tags).</summary>
    public static (bool Valid, string? Error) ValidateMatchQueryInput(string? userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput)) return (false, "userInput is required.");
        if (userInput.Length > MatchQueryUserInputMaxLength)
            return (false, $"userInput must be at most {MatchQueryUserInputMaxLength} characters.");
        if (ContainsScriptTags(userInput)) return (false, "userInput contains invalid content.");
        return (true, null);
    }

    /// <summary>Sanitize match-query user input (trim, strip scripts, cap length).</summary>
    public static string SanitizeMatchQueryInput(string? input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        var s = input;
        foreach (var tag in DangerousScriptTags)
            s = Regex.Replace(s, tag, "", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
        s = Regex.Replace(s, @"<[^>]*>", "", RegexOptions.None, TimeSpan.FromMilliseconds(100));
        if (s.Length > MatchQueryUserInputMaxLength) s = s[..MatchQueryUserInputMaxLength];
        return s.Trim();
    }

    /// <summary>Escape SQL-like wildcards for safe search: %, _, [, ].</summary>
    public static string EscapeSearchWildcards(string? input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return input
            .Replace("[", "[[]")
            .Replace("%", "[%]")
            .Replace("_", "[_]")
            .Replace("]", "[]]");
    }

    public static string SanitizeHtmlEntities(string? input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return input
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    public static string StripHtmlAndTrim(string? input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        var s = Regex.Replace(input, @"<[^>]*>", "", RegexOptions.None, TimeSpan.FromMilliseconds(100));
        return s.Trim();
    }

    private static bool ContainsDangerousSql(string input)
    {
        var lower = input.ToLowerInvariant();
        return DangerousSqlKeywords.Any(lower.Contains);
    }

    private static bool ContainsScriptTags(string input)
    {
        var lower = input.ToLowerInvariant();
        return DangerousScriptTags.Any(lower.Contains);
    }

    /// <summary>Prevent command injection: no backticks, no exec-like commands.</summary>
    private static bool ContainsCommandInjection(string input)
    {
        if (input.Contains('`')) return true;
        var lower = input.ToLowerInvariant();
        var dangerous = new[] { "exec(", "eval(", "system(", "shell(", "cmd(", "powershell", "bash ", "sh -" };
        return dangerous.Any(lower.Contains);
    }

    // Wizard: allowed variable values (prevent injection / arbitrary payloads)
    private static readonly HashSet<string> AllowedTimeslice = new(StringComparer.OrdinalIgnoreCase) { "5m", "15m", "30m", "1h" };
    private static readonly HashSet<string> AllowedDomain = new(StringComparer.OrdinalIgnoreCase) { "example.com", "another.com" };
    private static readonly HashSet<string> AllowedDomainPrefix = new(StringComparer.OrdinalIgnoreCase) { "www", "api" };
    private static readonly HashSet<string> AllowedEnvironment = new(StringComparer.OrdinalIgnoreCase) { "prod", "staging" };
    private static readonly HashSet<string> AllowedPanelKeys = new(StringComparer.OrdinalIgnoreCase)
        { "Success Rate %", "Error Rate %", "Slow Queries", "Past 7 day trend" };
    private static readonly HashSet<string> AllowedPanelCustomValues = new(StringComparer.OrdinalIgnoreCase)
        { "Request Success %", "Uptime %", "Health Check Pass Rate", "4xx/5xx Rate", "Exception Count", "Failed Request %",
          "Query Response Time", "Database Latency", "API Timeout Errors", "Custom Query", "Week-over-Week Change", "Rolling 7d Average", "Trend Analysis" };

    /// <summary>Validate wizard Variables and Panels before calling DashboardService. Do not trust AI or client payload blindly.</summary>
    public static (bool Valid, string? Error) ValidateWizardRequest(DashboardWizardRequest? request)
    {
        if (request == null) return (false, "Request is required.");
        var (titleValid, titleError) = ValidateDashboardName(request.DashboardTitle);
        if (!titleValid) return (false, titleError);

        if (!request.UseDefaults)
        {
            if (request.Variables == null)
                return (false, "Variables are required when not using defaults.");
            if (request.Variables.Timeslice != null && !AllowedTimeslice.Contains(request.Variables.Timeslice.Trim()))
                return (false, "Invalid Variables.Timeslice.");
            if (request.Variables.Domain != null && !AllowedDomain.Contains(request.Variables.Domain.Trim()))
                return (false, "Invalid Variables.Domain.");
            if (request.Variables.DomainPrefix != null && !AllowedDomainPrefix.Contains(request.Variables.DomainPrefix.Trim()))
                return (false, "Invalid Variables.DomainPrefix.");
            if (request.Variables.Environment != null && !AllowedEnvironment.Contains(request.Variables.Environment.Trim()))
                return (false, "Invalid Variables.Environment.");
        }

        if (request.Panels != null)
        {
            foreach (var kv in request.Panels)
            {
                var key = kv.Key?.Trim();
                if (string.IsNullOrEmpty(key)) continue;
                bool isCustomKey = key.EndsWith("_custom", StringComparison.OrdinalIgnoreCase);
                var baseKey = isCustomKey ? key[..^7].Trim() : key;
                if (!AllowedPanelKeys.Contains(baseKey))
                    return (false, $"Invalid panel key: {baseKey}.");
                if (isCustomKey && kv.Value is string s && !AllowedPanelCustomValues.Contains(s.Trim()))
                    return (false, $"Invalid panel custom value for {baseKey}.");
                if (!isCustomKey && kv.Value != null && !(kv.Value is bool))
                    return (false, $"Panel value for {baseKey} must be boolean.");
            }
        }

        return (true, null);
    }

    /// <summary>Sanitize AI or user-generated text before sending to client (XSS).</summary>
    public static string SanitizeForDisplay(string? input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        var s = input;
        foreach (var tag in DangerousScriptTags)
            s = Regex.Replace(s, tag, "", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
        s = Regex.Replace(s, @"<[^>]*>", "", RegexOptions.None, TimeSpan.FromMilliseconds(100));
        return s.Trim();
    }
}
