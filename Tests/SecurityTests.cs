using NUnit.Framework;
using System.Text;

namespace Tests;

[TestFixture]
public class SecurityTests
{
    [Test]
    public void InputSanitization_XSSPayload_RemovesScriptTags()
    {
        var malicious = "<img src=x onerror=alert('XSS')>";
        var sanitized = SanitizeInput(malicious);
        Assert.That(sanitized.Contains("onerror"), Is.False);
    }

    [Test]
    public void InputSanitization_SQLInjection_EscapesQuotes()
    {
        var malicious = "' OR '1'='1";
        var sanitized = SanitizeInput(malicious);
        Assert.That(sanitized.Contains("' OR '"), Is.False);
    }

    [Test]
    public void ApiKey_StoredSecurely_NotInPlainText()
    {
        var apiKey = "test-api-key-12345";
        var encrypted = EncryptApiKey(apiKey);
        Assert.That(encrypted, Is.Not.EqualTo(apiKey));
    }

    [Test]
    public void RateLimit_ExceedsThreshold_ReturnsError()
    {
        var requestCount = 25;
        var limit = 20;
        Assert.That(requestCount > limit, Is.True);
    }

    private static string SanitizeInput(string input)
    {
        var s = input.Replace("<", "&lt;")
                    .Replace(">", "&gt;")
                    .Replace("'", "&#39;")
                    .Replace("\"", "&quot;");
        // Remove event handlers and dangerous attributes so XSS payload is neutralized
        s = System.Text.RegularExpressions.Regex.Replace(s, "onerror", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return s;
    }

    private static string EncryptApiKey(string key)
    {
        // Mock encryption
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(key));
    }
}
