using System.Text;
using DashboardApi.Data;

namespace DashboardApi.Services;

/// <summary>
/// Matches natural language user input to the mock query library using Gemini (NLP/semantic matching).
/// </summary>
public class QueryMatchService
{
    private readonly GeminiChatService _gemini;

    public QueryMatchService(GeminiChatService gemini)
    {
        _gemini = gemini;
    }

    /// <summary>
    /// Match user input to the best query in the mock library. Returns match + explanation or no-match message.
    /// </summary>
    public async Task<QueryMatchResult> MatchQueryAsync(string userInput, CancellationToken cancellationToken = default)
    {
        if (!_gemini.IsConfigured())
        {
            return FallbackKeywordMatch(userInput ?? "");
        }

        var system = BuildSystemPrompt();
        var user = "User request: " + (userInput ?? "").Trim();

        try
        {
            var raw = await _gemini.GenerateWithSystemAsync(system, user, cancellationToken);
            return ParseMatchResponse(raw);
        }
        catch
        {
            return FallbackKeywordMatch(userInput ?? "");
        }
    }

    private static string BuildSystemPrompt()
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a Sumo Logic query assistant. Given the user's natural language request, select the MOST relevant query from the library below, or say no match.");
        sb.AppendLine("Do not include any personal or user-identifying information in your response (no PII in EXPLANATION).");
        sb.AppendLine();
        sb.AppendLine("LIBRARY (id, category, name, description, query):");
        foreach (var e in MockQueryLibrary.All)
        {
            sb.AppendLine($"- ID: {e.Id} | Category: {e.Category} | Name: {e.Name} | Description: {e.Description} | Query: {e.Query}");
        }
        sb.AppendLine();
        sb.AppendLine("Respond in this exact format:");
        sb.AppendLine("MATCHED_ID: <id from library, or leave blank if no match>");
        sb.AppendLine("EXPLANATION: <1-2 sentences why this query fits, or suggest what to ask>");
        sb.AppendLine("CONFIDENCE: high | medium | low");
        sb.AppendLine("If none of the queries fit the user's request, set MATCHED_ID to empty and in EXPLANATION give a helpful suggestion. Use NO_MATCH on its own line only when you are not selecting any query.");
        return sb.ToString();
    }

    private static QueryMatchResult ParseMatchResponse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return NoMatch("No matching query found. Try describing what you need.");

        var matchedId = "";
        var explanation = "";
        var confidence = "medium";

        var lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var t = line.Trim();
            if (t.StartsWith("MATCHED_ID:", StringComparison.OrdinalIgnoreCase))
                matchedId = t["MATCHED_ID:".Length..].Trim();
            else if (t.StartsWith("EXPLANATION:", StringComparison.OrdinalIgnoreCase))
                explanation = t["EXPLANATION:".Length..].Trim();
            else if (t.StartsWith("CONFIDENCE:", StringComparison.OrdinalIgnoreCase))
                confidence = t["CONFIDENCE:".Length..].Trim();
        }

        if (string.IsNullOrWhiteSpace(matchedId) || raw.Contains("NO_MATCH", StringComparison.OrdinalIgnoreCase))
            return NoMatch(string.IsNullOrWhiteSpace(explanation) ? "No matching query found. Try describing what you need." : explanation);

        var entry = MockQueryLibrary.GetById(matchedId);
        if (entry == null)
            return NoMatch(string.IsNullOrWhiteSpace(explanation) ? "No matching query found." : explanation);

        var confidenceValue = confidence.Trim().ToLowerInvariant() switch
        {
            "high" => 0.9,
            "low" => 0.5,
            _ => 0.7
        };

        return new QueryMatchResult
        {
            Matched = true,
            Query = entry.Query,
            Category = entry.Category,
            Explanation = string.IsNullOrWhiteSpace(explanation) ? $"This query matches your request: {entry.Name}." : explanation,
            Confidence = confidenceValue
        };
    }

    private static QueryMatchResult NoMatch(string message)
    {
        return new QueryMatchResult
        {
            Matched = false,
            Message = message
        };
    }

    /// <summary>Keyword fallback when Gemini is not configured or fails.</summary>
    private static QueryMatchResult FallbackKeywordMatch(string userInput)
    {
        var lower = (userInput ?? "").ToLowerInvariant();
        MockQueryEntry? best = null;
        if (lower.Contains("login") || lower.Contains("auth") || lower.Contains("session"))
            best = MockQueryLibrary.GetById("login-tracking");
        else if (lower.Contains("checkout") || lower.Contains("payment") || lower.Contains("fail"))
            best = MockQueryLibrary.GetById("checkout-failures");
        else if (lower.Contains("email") || lower.Contains("bounce") || lower.Contains("delivery"))
            best = MockQueryLibrary.GetById("email-delivery");
        else if (lower.Contains("slow") || lower.Contains("latency") || lower.Contains("page load"))
            best = MockQueryLibrary.GetById("slow-page-loads");

        if (best != null)
        {
            return new QueryMatchResult
            {
                Matched = true,
                Query = best.Query,
                Category = best.Category,
                Explanation = $"Keyword match: {best.Name}. For better results, set up the Gemini API key.",
                Confidence = 0.6
            };
        }

        return NoMatch("No matching query found. Try describing what you need (e.g. logins, checkout failures, email delivery, slow page loads).");
    }
}

public class QueryMatchResult
{
    public bool Matched { get; set; }
    public string? Query { get; set; }
    public string? Category { get; set; }
    public string? Explanation { get; set; }
    public double? Confidence { get; set; }
    public string? Message { get; set; }
}
