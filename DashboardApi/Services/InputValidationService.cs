using System.Text.RegularExpressions;

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
}
