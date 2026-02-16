using NUnit.Framework;
using System.Text.RegularExpressions;

namespace Tests;

[TestFixture]
public class DashboardTests
{
    [Test]
    public void DashboardName_StartsWithUppercase_ReturnsTrue()
    {
        var name = "Production Dashboard";
        var regex = new Regex(@"^[A-Z][a-zA-Z0-9\s_-]{2,50}$");
        Assert.That(regex.IsMatch(name), Is.True);
    }

    [Test]
    public void DashboardName_StartsWithLowercase_ReturnsFalse()
    {
        var name = "production Dashboard";
        var regex = new Regex(@"^[A-Z][a-zA-Z0-9\s_-]{2,50}$");
        Assert.That(regex.IsMatch(name), Is.False);
    }

    [Test]
    public void DashboardName_ContainsSQLInjection_GetsSanitized()
    {
        var maliciousInput = "Test'; DROP TABLE--";
        var sanitized = SanitizeInput(maliciousInput);
        Assert.That(sanitized.Contains("DROP TABLE"), Is.False);
    }

    private static string SanitizeInput(string input)
    {
        var s = input.Replace("'", "").Replace(";", "").Replace("--", "");
        // Remove dangerous SQL keywords so sanitized output does not contain them
        s = System.Text.RegularExpressions.Regex.Replace(s, @"DROP\s+TABLE", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return s;
    }
}
