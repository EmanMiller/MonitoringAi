using System.Security.Claims;
using DashboardApi.Data;
using DashboardApi.Models;
using DashboardApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace DashboardApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly GeminiChatService _chatService;
    private readonly ChatRateLimitService _rateLimit;
    private readonly QueryAssistantAiService _queryAi;
    private readonly DashboardFlowService _dashboardFlow;
    private readonly QueryMatchService _queryMatch;

    public ChatController(GeminiChatService chatService, ChatRateLimitService rateLimit, QueryAssistantAiService queryAi, DashboardFlowService dashboardFlow, QueryMatchService queryMatch)
    {
        _chatService = chatService;
        _rateLimit = rateLimit;
        _queryAi = queryAi;
        _dashboardFlow = dashboardFlow;
        _queryMatch = queryMatch;
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
            var safeText = InputValidationService.SanitizeForDisplay(responseText);
            return Ok(new ChatResponse { Response = safeText });
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

    /// <summary>
    /// Match user natural language to a query from the mock library. Body: { userInput }. Rate limited.
    /// </summary>
    [HttpPost("match-query")]
    public async Task<ActionResult<QueryMatchResult>> MatchQuery([FromBody] MatchQueryRequest request)
    {
        var (valid, error) = InputValidationService.ValidateMatchQueryInput(request?.UserInput);
        if (!valid)
            return BadRequest(new { details = error ?? "userInput is required." });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var (allowed, retryAfter) = _rateLimit.TryConsume(userId);
        if (!allowed)
        {
            Response.Headers.RetryAfter = retryAfter.ToString();
            return StatusCode(429, new { details = "Too many requests. Please wait.", retryAfterSeconds = retryAfter });
        }

        var sanitized = InputValidationService.SanitizeMatchQueryInput(request!.UserInput);
        var result = await _queryMatch.MatchQueryAsync(sanitized);
        var safe = new QueryMatchResult
        {
            Matched = result.Matched,
            Query = result.Query != null ? InputValidationService.SanitizeForDisplay(result.Query) : null,
            Category = result.Category != null ? InputValidationService.SanitizeForDisplay(result.Category) : null,
            Explanation = result.Explanation != null ? InputValidationService.SanitizeForDisplay(result.Explanation) : null,
            Confidence = result.Confidence,
            Message = result.Message != null ? InputValidationService.SanitizeForDisplay(result.Message) : null
        };
        return Ok(safe);
    }

    /// <summary>
    /// Natural language → Sumo Logic query + explanation. Body: { userInput, context }.
    /// </summary>
    [HttpPost("generate-query")]
    public async Task<ActionResult<GenerateQueryResponse>> GenerateQuery([FromBody] GenerateQueryRequest request)
    {
        if (!_chatService.IsConfigured())
            return StatusCode(503, new { error = "Gemini API not configured." });
        var (allowed, retryAfter) = _rateLimit.TryConsume(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anon");
        if (!allowed)
        {
            Response.Headers.RetryAfter = retryAfter.ToString();
            return StatusCode(429, new { details = "Too many requests. Please wait.", retryAfterSeconds = retryAfter });
        }
        var userInput = InputValidationService.SanitizeChatMessage(request?.UserInput);
        if (string.IsNullOrWhiteSpace(userInput))
            return BadRequest(new { details = "userInput is required." });
        if (userInput.Length > 2000)
            return BadRequest(new { details = "userInput must be at most 2000 characters." });
        try
        {
            var result = await _queryAi.GenerateQueryAsync(userInput, request?.Context, HttpContext.RequestAborted);
            return Ok(new GenerateQueryResponse
            {
                Query = result.Query,
                Explanation = result.Explanation,
                Confidence = result.Confidence
            });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(503, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Suggest 3–5 optimizations for a query. Body: { query, performance }.
    /// </summary>
    [HttpPost("optimize-query")]
    public async Task<ActionResult<OptimizeQueryResponse>> OptimizeQuery([FromBody] OptimizeQueryRequest request)
    {
        if (!_chatService.IsConfigured())
            return StatusCode(503, new { error = "Gemini API not configured." });
        var (allowed, retryAfter) = _rateLimit.TryConsume(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anon");
        if (!allowed)
        {
            Response.Headers.RetryAfter = retryAfter.ToString();
            return StatusCode(429, new { details = "Too many requests. Please wait.", retryAfterSeconds = retryAfter });
        }
        var query = InputValidationService.SanitizeChatMessage(request?.Query);
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(new { details = "query is required." });
        if (query.Length > 8000)
            return BadRequest(new { details = "query must be at most 8000 characters." });
        try
        {
            var result = await _queryAi.OptimizeQueryAsync(query, request?.Performance, HttpContext.RequestAborted);
            var dtos = result.Suggestions.Select(s => new QuerySuggestionDto { Suggestion = s.Suggestion, Impact = s.Impact, Reason = s.Reason }).ToList();
            return Ok(new OptimizeQueryResponse { Suggestions = dtos });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(503, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Plain English, step-by-step explanation of a Sumo Logic query. Body: { query }.
    /// </summary>
    [HttpPost("explain-query")]
    public async Task<ActionResult<ExplainQueryResponse>> ExplainQuery([FromBody] ExplainQueryRequest request)
    {
        if (!_chatService.IsConfigured())
            return StatusCode(503, new { error = "Gemini API not configured." });
        var (allowed, retryAfter) = _rateLimit.TryConsume(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anon");
        if (!allowed)
        {
            Response.Headers.RetryAfter = retryAfter.ToString();
            return StatusCode(429, new { details = "Too many requests. Please wait.", retryAfterSeconds = retryAfter });
        }
        var query = InputValidationService.SanitizeChatMessage(request?.Query);
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(new { details = "query is required." });
        if (query.Length > 8000)
            return BadRequest(new { details = "query must be at most 8000 characters." });
        try
        {
            var result = await _queryAi.ExplainQueryAsync(query, HttpContext.RequestAborted);
            return Ok(new ExplainQueryResponse { Explanation = result.Explanation, Confidence = result.Confidence });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(503, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Dashboard creation conversation: send user message with optional flowContext and history; get response plus optional step data or complete payload.
    /// </summary>
    [HttpPost("dashboard-flow")]
    public async Task<ActionResult<DashboardFlowResponse>> DashboardFlow([FromBody] DashboardFlowRequest request)
    {
        if (!_chatService.IsConfigured())
            return StatusCode(503, new { error = "Gemini API not configured." });
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var (allowed, retryAfter) = _rateLimit.TryConsume(userId);
        if (!allowed)
        {
            Response.Headers.RetryAfter = retryAfter.ToString();
            return StatusCode(429, new { details = "Too many requests. Please wait.", retryAfterSeconds = retryAfter });
        }
        var message = InputValidationService.SanitizeChatMessage(request?.Message);
        if (string.IsNullOrWhiteSpace(message))
            return BadRequest(new { details = "message is required." });
        if (message.Length > 2000)
            return BadRequest(new { details = "message must be at most 2000 characters." });

        var historyItems = request?.History?.Select(h => new DashboardFlowHistoryItem { Sender = h.Sender, Text = InputValidationService.SanitizeChatMessage(h.Text) }).ToList();

        DashboardFlowContext? flowContext = null;
        if (request?.FlowContext != null)
        {
            var fc = request.FlowContext;
            flowContext = new DashboardFlowContext
            {
                Step = fc.Step,
                Collected = fc.Collected == null ? null : new DashboardCollected
                {
                    DashboardTitle = fc.Collected.DashboardTitle,
                    UseDefaults = fc.Collected.UseDefaults,
                    Variables = fc.Collected.Variables == null ? null : new TemplateVariables
                    {
                        Timeslice = fc.Collected.Variables.Timeslice,
                        Domain = fc.Collected.Variables.Domain,
                        DomainPrefix = fc.Collected.Variables.DomainPrefix,
                        Environment = fc.Collected.Variables.Environment
                    },
                    Panels = fc.Collected.Panels
                }
            };
        }

        try
        {
            var result = await _dashboardFlow.ProcessAsync(message, flowContext, historyItems, HttpContext.RequestAborted);
            var stepData = result.StepData == null ? null : new DashboardStepDataDto
            {
                Step = result.StepData.Step,
                Prompt = result.StepData.Prompt,
                Type = result.StepData.Type,
                Options = result.StepData.Options
            };
            return Ok(new DashboardFlowResponse
            {
                ResponseText = result.ResponseText,
                StepData = stepData,
                CompletePayload = result.CompletePayload
            });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(503, new { error = ex.Message });
        }
    }
}

public class MatchQueryRequest
{
    public string? UserInput { get; set; }
}

public class MatchQueryResponse
{
    public bool Matched { get; set; }
    public string? MatchedId { get; set; }
    public string? Category { get; set; }
    public string? Query { get; set; }
    public string? Explanation { get; set; }
    public string? Confidence { get; set; }
    public string? Message { get; set; }
}

public class GenerateQueryRequest
{
    public string? UserInput { get; set; }
    public string? Context { get; set; }
}

public class GenerateQueryResponse
{
    public string Query { get; set; } = "";
    public string Explanation { get; set; } = "";
    public string Confidence { get; set; } = "high";
}

public class OptimizeQueryRequest
{
    public string? Query { get; set; }
    public string? Performance { get; set; }
}

public class OptimizeQueryResponse
{
    public List<QuerySuggestionDto> Suggestions { get; set; } = new();
}

public class QuerySuggestionDto
{
    public string Suggestion { get; set; } = "";
    public string Impact { get; set; } = "Medium";
    public string Reason { get; set; } = "";
}

public class ExplainQueryRequest
{
    public string? Query { get; set; }
}

public class ExplainQueryResponse
{
    public string Explanation { get; set; } = "";
    public string Confidence { get; set; } = "high";
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

public class DashboardFlowRequest
{
    public string? Message { get; set; }
    public DashboardFlowContextDto? FlowContext { get; set; }
    public List<ChatTurnDto>? History { get; set; }
}

public class DashboardFlowContextDto
{
    public int? Step { get; set; }
    public DashboardCollectedDto? Collected { get; set; }
}

public class DashboardCollectedDto
{
    public string? DashboardTitle { get; set; }
    public bool? UseDefaults { get; set; }
    public TemplateVariablesDto? Variables { get; set; }
    public Dictionary<string, object>? Panels { get; set; }
}

public class TemplateVariablesDto
{
    public string? Timeslice { get; set; }
    public string? Domain { get; set; }
    public string? DomainPrefix { get; set; }
    public string? Environment { get; set; }
}

public class DashboardFlowResponse
{
    public string ResponseText { get; set; } = "";
    public DashboardStepDataDto? StepData { get; set; }
    public DashboardWizardRequest? CompletePayload { get; set; }
}

public class DashboardStepDataDto
{
    public int Step { get; set; }
    public string? Prompt { get; set; }
    public string? Type { get; set; }
    public List<string>? Options { get; set; }
}
