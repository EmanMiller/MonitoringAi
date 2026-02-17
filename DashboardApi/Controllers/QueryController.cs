using System.Security.Claims;
using DashboardApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DashboardApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QueryController : ControllerBase
{
    private readonly QueryAssistantService _queryAssistant;
    private readonly ChatRateLimitService _chatRateLimit;
    private readonly IActivityService _activityService;

    public QueryController(QueryAssistantService queryAssistant, ChatRateLimitService chatRateLimit, IActivityService activityService)
    {
        _queryAssistant = queryAssistant;
        _chatRateLimit = chatRateLimit;
        _activityService = activityService;
    }

    [HttpPost("ask")]
    public async Task<ActionResult<QueryResponse>> Ask([FromBody] QueryRequest request)
    {
        var (valid, error) = InputValidationService.ValidateChatMessage(request?.Message);
        if (!valid) return BadRequest(new { details = error ?? "Message is required." });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var (allowed, retryAfter) = _chatRateLimit.TryConsume(userId);
        if (!allowed)
        {
            Response.Headers.RetryAfter = retryAfter.ToString();
            return StatusCode(429, new { details = "Too many messages. Please wait before sending more.", retryAfterSeconds = retryAfter });
        }

        var sanitized = InputValidationService.SanitizeChatMessage(request!.Message);
        var redacted = InputValidationService.StripPii(sanitized);
        try
        {
            var queryText = await _queryAssistant.GetSumoQueryAsync(redacted);
            var label = redacted.Length > 50 ? redacted[..47] + "..." : redacted;
            _activityService.LogActivity("query_run", $"Query '{label}' ran successfully", userId);
            return Ok(new QueryResponse { QueryText = queryText });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { details = ex.Message });
        }
    }

    [HttpPost("run")]
    public IActionResult Run([FromBody] QueryRunRequest request)
    {
        return StatusCode(501, new
        {
            message = "Query execution not yet implemented. Sumo Logic integration pending.",
            rows = Array.Empty<object>(),
            columns = Array.Empty<string>(),
            rowCount = 0,
            executionTimeMs = 0
        });
    }
}

public class QueryRunRequest
{
    public string? Query { get; set; }
    public string? TimeRange { get; set; }
    public int? Limit { get; set; }
}

public class QueryRequest
{
    public string? Message { get; set; }
}

public class QueryResponse
{
    public string QueryText { get; set; } = "";
}
