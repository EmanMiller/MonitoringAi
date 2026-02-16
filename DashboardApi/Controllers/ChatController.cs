using System.Security.Claims;
using DashboardApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DashboardApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly GeminiChatService _chatService;
    private readonly ChatRateLimitService _rateLimit;

    public ChatController(GeminiChatService chatService, ChatRateLimitService rateLimit)
    {
        _chatService = chatService;
        _rateLimit = rateLimit;
    }

    /// <summary>
    /// Check if chat is available (Gemini API key configured). No key exposed.
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var configured = _chatService.IsConfigured();
        if (configured)
            return Ok(new { configured = true, isConfigured = true });
        return StatusCode(503, new { configured = false, isConfigured = false, details = "Chat is not configured. Set GEMINI_API_KEY or Gemini:ApiKey on the server." });
    }

    /// <summary>
    /// Send a message and get Gemini reply. History for context. Rate limit: 20/min per user.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Post([FromBody] ChatRequest request)
    {
        var (valid, error) = InputValidationService.ValidateChatMessage(request?.Message);
        if (!valid)
            return BadRequest(new { details = error ?? "Message is required." });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var (allowed, retryAfter) = _rateLimit.TryConsume(userId);
        if (!allowed)
        {
            Response.Headers.RetryAfter = retryAfter.ToString();
            return StatusCode(429, new { details = "Too many requests. Please wait.", retryAfterSeconds = retryAfter });
        }

        var history = NormalizeHistory(request!.History);
        var sanitized = InputValidationService.SanitizeChatMessage(request.Message);

        try
        {
            var responseText = await _chatService.SendChatAsync(sanitized, history);
            return Ok(new ChatResponse { Response = responseText });
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("API key") || ex.Message.Contains("not configured"))
                return StatusCode(503, new { details = "API key invalid. Check settings." });
            if (ex.Message.Contains("Too many requests"))
                return StatusCode(429, new { details = "Too many requests. Wait 1 minute.", retryAfterSeconds = 60 });
            return StatusCode(503, new { details = "Gemini unavailable. Check API key." });
        }
    }

    private static IReadOnlyList<ChatTurn> NormalizeHistory(IEnumerable<ChatTurnDto>? history)
    {
        if (history == null) return Array.Empty<ChatTurn>();
        var list = new List<ChatTurn>();
        foreach (var h in history.Take(50))
        {
            var sender = (h.Sender ?? "").Trim().ToLowerInvariant();
            if (sender != "user" && sender != "assistant") sender = "user";
            var text = InputValidationService.SanitizeChatMessage(h.Text);
            if (string.IsNullOrEmpty(text)) continue;
            list.Add(new ChatTurn { Sender = sender, Text = text });
        }
        return list;
    }
}

public class ChatRequest
{
    public string? Message { get; set; }
    public List<ChatTurnDto>? History { get; set; }
}

public class ChatTurnDto
{
    public string? Sender { get; set; }
    public string? Text { get; set; }
}

public class ChatResponse
{
    public string Response { get; set; } = "";
}
