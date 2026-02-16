using Newtonsoft.Json;

namespace DashboardApi.Services;

/// <summary>
/// Conversational chat via Gemini API. API key is server-side only (Gemini:ApiKey).
/// </summary>
public class GeminiChatService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GeminiChatService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    /// <summary>
    /// Send a message with conversation history. Returns assistant reply text.
    /// </summary>
    public async Task<string> SendChatAsync(string message, IReadOnlyList<ChatTurn> history, CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["Gemini:ApiKey"];
        var modelName = _configuration["Gemini:Model"] ?? "gemini-2.0-flash";
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("Gemini:ApiKey is not configured.");

        var contents = new List<object>();
        foreach (var turn in history)
        {
            var role = turn.Sender == "user" ? "user" : "model";
            contents.Add(new { role, parts = new[] { new { text = turn.Text ?? "" } } });
        }
        contents.Add(new { role = "user", parts = new[] { new { text = message } } });

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";
        var body = new
        {
            contents,
            generationConfig = new
            {
                temperature = 0.7,
                maxOutputTokens = 2048,
                topP = 0.95,
                topK = 40
            }
        };
        var json = JsonConvert.SerializeObject(body);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new InvalidOperationException("API key invalid. Check settings.");
        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
            (int)response.StatusCode == 429)
            throw new InvalidOperationException("Too many requests. Wait 1 minute.");
        if (!response.IsSuccessStatusCode)
        {
            var errBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Gemini unavailable. Check API key. ({response.StatusCode})");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var parsed = JsonConvert.DeserializeObject<dynamic>(responseJson);
        var text = parsed?.candidates?[0]?.content?.parts?[0]?.text?.ToString()?.Trim();
        if (string.IsNullOrEmpty(text))
            throw new InvalidOperationException("Gemini returned no response.");
        return text!;
    }

    /// <summary>
    /// Returns true if Gemini is configured (API key set).
    /// </summary>
    public bool IsConfigured()
    {
        var apiKey = _configuration["Gemini:ApiKey"];
        return !string.IsNullOrWhiteSpace(apiKey);
    }
}

public class ChatTurn
{
    public string Sender { get; set; } = ""; // "user" | "assistant"
    public string Text { get; set; } = "";
}
