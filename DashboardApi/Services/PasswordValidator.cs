using System.Text.RegularExpressions;

namespace DashboardApi.Services;

/// <summary>
/// Password requirements: min 12 chars, uppercase, lowercase, number, special character.
/// Never log or expose passwords.
/// </summary>
public static class PasswordValidator
{
    private static readonly Regex Upper = new(@"[A-Z]", RegexOptions.Compiled, TimeSpan.FromMilliseconds(50));
    private static readonly Regex Lower = new(@"[a-z]", RegexOptions.Compiled, TimeSpan.FromMilliseconds(50));
    private static readonly Regex Digit = new(@"\d", RegexOptions.Compiled, TimeSpan.FromMilliseconds(50));
    private static readonly Regex Special = new(@"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>/?`~]", RegexOptions.Compiled, TimeSpan.FromMilliseconds(50));
    private const int MinLength = 12;

    public static (bool Valid, string? Error) Validate(string? password)
    {
        if (string.IsNullOrEmpty(password)) return (false, "Password is required.");
        if (password.Length < MinLength) return (false, $"Password must be at least {MinLength} characters.");
        if (!Upper.IsMatch(password)) return (false, "Password must include at least one uppercase letter.");
        if (!Lower.IsMatch(password)) return (false, "Password must include at least one lowercase letter.");
        if (!Digit.IsMatch(password)) return (false, "Password must include at least one number.");
        if (!Special.IsMatch(password)) return (false, "Password must include at least one special character.");
        return (true, null);
    }

    public static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, 12);
    }
}
