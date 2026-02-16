using DashboardApi.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;

namespace DashboardApi.Services;

public class QueryAssistantService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;

    public QueryAssistantService(
        HttpClient httpClient,
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
    }

    public async Task<string> GetSumoQueryAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["GEMINI_API_KEY"] ?? _configuration["Gemini:ApiKey"];
        var trimmed = apiKey?.Trim();
        if (string.IsNullOrEmpty(trimmed) || string.Equals(trimmed, "placeholder", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Gemini API key is not configured. Set GEMINI_API_KEY or Gemini:ApiKey.");
        var model = _configuration["Gemini:Model"] ?? "gemini-2.0-flash";

        List<LogMapping> mappings;
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            mappings = await db.LogMappings.Where(x => x.IsActive).ToListAsync(cancellationToken);
        }

        var systemInstruction = BuildSystemPrompt(mappings);

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={trimmed}";
        var body = new
        {
            systemInstruction = new
            {
                parts = new[] { new { text = systemInstruction } }
            },
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = userMessage } }
                }
            },
            generationConfig = new
            {
                temperature = 0.2,
                maxOutputTokens = 1024,
                topP = 0.95,
                topK = 40
            }
        };
        var json = JsonConvert.SerializeObject(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var parsed = JsonConvert.DeserializeObject<dynamic>(responseJson);
        var text = parsed?.candidates?[0]?.content?.parts?[0]?.text?.ToString()?.Trim();
        if (string.IsNullOrEmpty(text))
            throw new InvalidOperationException("Gemini returned no query text.");
        return text;
    }

    private static string BuildSystemPrompt(List<LogMapping> mappings)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a Sumo Logic query assistant. Your only job is to output a valid Sumo Logic query based on the user's natural language request.");
        sb.AppendLine();
        sb.AppendLine("CRITICAL: Return ONLY raw Sumo Logic query text. No filler, no explanations, no markdown, no backticks.");
        sb.AppendLine();
        if (mappings.Count > 0)
        {
            sb.AppendLine("--- MAPPING CHEAT SHEET (use these when the user mentions the Key) ---");
            foreach (var m in mappings)
                sb.AppendLine($"- When the user says or implies \"{m.Key}\", the query must use: {m.Value}");
            sb.AppendLine("--- END MAPPINGS ---");
            sb.AppendLine();
        }
        sb.AppendLine("Examples: user says 'prod errors' -> use the Environment/Intent mapping for 'prod' and include error-related filters in the query.");
        return sb.ToString();
    }
}
