using System.Text;
using System.Text.RegularExpressions;

namespace DashboardApi.Services;

/// <summary>
/// AI-powered query builder: natural language → query, optimize, explain. Uses Gemini with structured prompts.
/// </summary>
public class QueryAssistantAiService
{
    private readonly GeminiChatService _gemini;

    public QueryAssistantAiService(GeminiChatService gemini)
    {
        _gemini = gemini;
    }

    /// <summary>
    /// Natural language to Sumo Logic query + explanation.
    /// </summary>
    public async Task<GenerateQueryResult> GenerateQueryAsync(string userInput, string? context, CancellationToken cancellationToken = default)
    {
        var system = BuildGenerateQuerySystemPrompt();
        var user = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(context))
            user.AppendLine("Available context (fields/sources to consider):").AppendLine(context).AppendLine();
        user.Append("User request: ").Append(userInput?.Trim() ?? "");

        var raw = await _gemini.GenerateWithSystemAsync(system, user.ToString(), cancellationToken);
        return ParseGenerateQueryResponse(raw);
    }

    /// <summary>
    /// Suggest 3–5 optimizations with Impact and Reason.
    /// </summary>
    public async Task<OptimizeQueryResult> OptimizeQueryAsync(string query, string? performance, CancellationToken cancellationToken = default)
    {
        var system = BuildOptimizeQuerySystemPrompt();
        var user = new StringBuilder();
        user.AppendLine("Query to optimize:").AppendLine(query).AppendLine();
        if (!string.IsNullOrWhiteSpace(performance))
            user.AppendLine("Performance notes (if any):").AppendLine(performance).AppendLine();
        user.Append("Return 3–5 specific suggestions.");

        var raw = await _gemini.GenerateWithSystemAsync(system, user.ToString(), cancellationToken);
        return ParseOptimizeQueryResponse(raw);
    }

    /// <summary>
    /// Plain English, step-by-step explanation of a Sumo Logic query.
    /// </summary>
    public async Task<ExplainQueryResult> ExplainQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        var system = BuildExplainQuerySystemPrompt();
        var user = "Explain this Sumo Logic query in plain English, step by step, for someone new to the platform:\n\n" + (query?.Trim() ?? "");

        var raw = await _gemini.GenerateWithSystemAsync(system, user, cancellationToken);
        return new ExplainQueryResult
        {
            Explanation = raw?.Trim() ?? "No explanation generated.",
            Confidence = "high"
        };
    }

    /// <summary>
    /// NLP/semantic match: map user natural language to the most relevant query from the library. Handles paraphrasing; returns no-match when none fit.
    /// </summary>
    public async Task<MatchQueryResult> MatchQueryAsync(string userInput, IReadOnlyList<QueryLibraryEntryForMatch> library, CancellationToken cancellationToken = default)
    {
        if (library == null || library.Count == 0)
            return new MatchQueryResult { Matched = false, Message = "No query library available." };
        var system = BuildMatchQuerySystemPrompt(library);
        var user = "User request: " + (userInput?.Trim() ?? "");
        var raw = await _gemini.GenerateWithSystemAsync(system, user, cancellationToken);
        return ParseMatchQueryResponse(raw, library);
    }

    private static string BuildMatchQuerySystemPrompt(IReadOnlyList<QueryLibraryEntryForMatch> library)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a Sumo Logic query assistant. Given the user's natural language request, select the MOST relevant query from the library below. Handle paraphrasing (e.g. 'show me failed checkouts', 'email bounce rate').");
        sb.AppendLine();
        sb.AppendLine("--- QUERY LIBRARY (use only these) ---");
        foreach (var e in library)
            sb.AppendLine($"ID: {e.Id} | Category: {e.Category} | Name: {e.Name} | Description: {e.Description} | Query: {e.Query}");
        sb.AppendLine("--- END LIBRARY ---");
        sb.AppendLine();
        sb.AppendLine("If one query clearly fits, respond with:");
        sb.AppendLine("MATCHED_ID: <id from library>");
        sb.AppendLine("QUERY: <full query string from library>");
        sb.AppendLine("EXPLANATION: <1-2 sentences why this matches the user's request>");
        sb.AppendLine("CONFIDENCE: high|medium|low");
        sb.AppendLine();
        sb.AppendLine("If NONE of the queries fit the user's request, respond with:");
        sb.AppendLine("MATCHED: false");
        sb.AppendLine("MESSAGE: <short helpful suggestion, e.g. what they could ask instead>");
        sb.AppendLine();
        sb.AppendLine("Use only the exact IDs and Query strings from the library. Return exactly one of the two formats above.");
        return sb.ToString();
    }

    private static MatchQueryResult ParseMatchQueryResponse(string raw, IReadOnlyList<QueryLibraryEntryForMatch> library)
    {
        var noMatch = Regex.IsMatch(raw ?? "", @"MATCHED:\s*false", RegexOptions.IgnoreCase);
        if (noMatch)
        {
            var msgMatch = Regex.Match(raw ?? "", @"MESSAGE:\s*(.+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var message = msgMatch.Success ? msgMatch.Groups[1].Value.Trim() : "No matching query found. Try describing what you need (e.g. logins, checkouts, email, slow requests).";
            return new MatchQueryResult { Matched = false, Message = message };
        }

        var idMatch = Regex.Match(raw ?? "", @"MATCHED_ID:\s*(\S+)", RegexOptions.IgnoreCase);
        var id = idMatch.Success ? idMatch.Groups[1].Value.Trim() : "";
        var entry = library.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));

        var queryMatch = Regex.Match(raw ?? "", @"QUERY:\s*(.+?)(?=EXPLANATION:|CONFIDENCE:|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var query = queryMatch.Success ? queryMatch.Groups[1].Value.Trim() : (entry?.Query ?? "");
        if (string.IsNullOrWhiteSpace(query) && entry != null)
            query = entry.Query;

        var explMatch = Regex.Match(raw ?? "", @"EXPLANATION:\s*(.+?)(?=CONFIDENCE:|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var explanation = explMatch.Success ? explMatch.Groups[1].Value.Trim() : "";

        var confMatch = Regex.Match(raw ?? "", @"CONFIDENCE:\s*(high|medium|low)", RegexOptions.IgnoreCase);
        var confidence = confMatch.Success ? confMatch.Groups[1].Value.ToLowerInvariant() : "medium";

        if (entry == null && !string.IsNullOrWhiteSpace(id))
            entry = library.FirstOrDefault(e => raw?.Contains(e.Query, StringComparison.Ordinal) == true);

        return new MatchQueryResult
        {
            Matched = entry != null || !string.IsNullOrWhiteSpace(query),
            MatchedId = entry?.Id ?? id,
            Category = entry?.Category ?? "",
            Query = query,
            Explanation = explanation,
            Confidence = confidence,
            Message = null
        };
    }

    private static string BuildGenerateQuerySystemPrompt()
    {
        return """
You are a Sumo Logic query expert. Convert the user's natural language into a valid Sumo Logic query and give a short explanation.

Sumo Logic syntax reminders:
- Use _sourceCategory, _sourceHost, _sourceName for filtering logs
- Time range is often specified in the UI; in query use | where _receiptTime or time filters as needed
- Common: parse "key=*", | where condition, | count, | timeslice
- Use backticks for exact field names when needed

You MUST respond in this exact format (so the parser can extract it):

QUERY:
<put the raw Sumo Logic query here, single block>

EXPLANATION:
<1-3 sentences in plain English describing what the query does>

If you are unsure about syntax, say so briefly in EXPLANATION and still provide a best-effort QUERY.
""";
    }

    private static string BuildOptimizeQuerySystemPrompt()
    {
        return """
You are a Sumo Logic performance expert. Given a query and optional performance notes, suggest 3–5 specific improvements.

For each suggestion provide:
1. Suggestion (one clear action)
2. Impact: High, Medium, or Low
3. Reason (one sentence)

Respond in this format only (one block per suggestion):

SUGGESTION: <short action>
IMPACT: High|Medium|Low
REASON: <one sentence>

Repeat for each suggestion. Example:
SUGGESTION: Add a time filter (e.g. _receiptTime) to reduce scan range
IMPACT: High
REASON: Scanning only recent data speeds up the query.

Common optimizations: add time filter, use index/full search hints, combine where clauses, add limit, avoid wildcards at start of term, use parse early to filter.
""";
    }

    private static string BuildExplainQuerySystemPrompt()
    {
        return """
You are a Sumo Logic educator. Explain the given Sumo Logic query in plain English, step by step, for beginners.

Structure your answer as a short intro sentence, then numbered steps (1. 2. 3. ...) for each major part of the query (e.g. what each pipeline stage does). Keep it clear and concise. Do not include code unless it's a direct quote of a keyword from the query.
""";
    }

    private static GenerateQueryResult ParseGenerateQueryResponse(string raw)
    {
        var query = "";
        var explanation = "";
        var confidence = "high";

        var queryMatch = Regex.Match(raw, @"QUERY:\s*(.+?)(?=EXPLANATION:|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (queryMatch.Success)
            query = queryMatch.Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            var fallback = Regex.Match(raw, @"```(?:\w+)?\s*([\s\S]*?)```");
            if (fallback.Success)
                query = fallback.Groups[1].Value.Trim();
        }
        if (string.IsNullOrWhiteSpace(query))
            query = raw.Trim();

        var explMatch = Regex.Match(raw, @"EXPLANATION:\s*(.+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (explMatch.Success)
            explanation = explMatch.Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(explanation) && raw.Length > query.Length)
            explanation = raw.Replace(query, "").Trim();

        if (raw.Contains("unsure", StringComparison.OrdinalIgnoreCase) || raw.Contains("best-effort", StringComparison.OrdinalIgnoreCase))
            confidence = "low";

        return new GenerateQueryResult
        {
            Query = query,
            Explanation = string.IsNullOrWhiteSpace(explanation) ? "No explanation provided." : explanation,
            Confidence = confidence
        };
    }

    private static OptimizeQueryResult ParseOptimizeQueryResponse(string raw)
    {
        var suggestions = new List<QuerySuggestion>();
        var blocks = Regex.Split(raw, @"\bSUGGESTION:\s*", RegexOptions.IgnoreCase);
        foreach (var block in blocks.Skip(1))
        {
            var lines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var suggestion = lines.Length > 0 ? lines[0].Trim() : "";
            var impact = "Medium";
            var reason = "";
            for (var i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith("IMPACT:", StringComparison.OrdinalIgnoreCase))
                {
                    if (line.Contains("High", StringComparison.OrdinalIgnoreCase)) impact = "High";
                    else if (line.Contains("Low", StringComparison.OrdinalIgnoreCase)) impact = "Low";
                }
                else if (line.StartsWith("REASON:", StringComparison.OrdinalIgnoreCase))
                    reason = line["REASON:".Length..].Trim();
            }
            if (!string.IsNullOrWhiteSpace(suggestion))
                suggestions.Add(new QuerySuggestion { Suggestion = suggestion, Impact = impact, Reason = reason });
        }

        if (suggestions.Count == 0)
        {
            foreach (var line in raw.Split('\n', StringSplitOptions.RemoveEmptyEntries).Take(5))
            {
                var t = line.Trim();
                if (t.Length > 15)
                    suggestions.Add(new QuerySuggestion { Suggestion = t, Impact = "Medium", Reason = "" });
            }
        }

        return new OptimizeQueryResult { Suggestions = suggestions };
    }
}

public class GenerateQueryResult
{
    public string Query { get; set; } = "";
    public string Explanation { get; set; } = "";
    public string Confidence { get; set; } = "high";
}

public class OptimizeQueryResult
{
    public List<QuerySuggestion> Suggestions { get; set; } = new();
}

public class QuerySuggestion
{
    public string Suggestion { get; set; } = "";
    public string Impact { get; set; } = "Medium";
    public string Reason { get; set; } = "";
}

public class ExplainQueryResult
{
    public string Explanation { get; set; } = "";
    public string Confidence { get; set; } = "high";
}

public class QueryLibraryEntryForMatch
{
    public string Id { get; set; } = "";
    public string Category { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Query { get; set; } = "";
}

public class MatchQueryResult
{
    public bool Matched { get; set; }
    public string? MatchedId { get; set; }
    public string Category { get; set; } = "";
    public string Query { get; set; } = "";
    public string Explanation { get; set; } = "";
    public string Confidence { get; set; } = "medium";
    public string? Message { get; set; }
}
