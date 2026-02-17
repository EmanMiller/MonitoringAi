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
    private readonly SumoLogicQueryService _sumoLogicQuery;
    private readonly ChatRateLimitService _chatRateLimit;
    private readonly IActivityService _activityService;

    public QueryController(QueryAssistantService queryAssistant, SumoLogicQueryService sumoLogicQuery, ChatRateLimitService chatRateLimit, IActivityService activityService)
    {
        _queryAssistant = queryAssistant;
        _sumoLogicQuery = sumoLogicQuery;
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
    [Authorize]
    public async Task<IActionResult> Run([FromBody] QueryRunRequest request)
    {
        var query = request?.Query?.Trim();
        if (string.IsNullOrEmpty(query))
            return BadRequest(new { message = "Query is required.", rows = Array.Empty<object>(), columns = Array.Empty<string>(), rowCount = 0, executionTimeMs = 0L });

        var result = await _sumoLogicQuery.ExecuteQueryAsync(query, request?.TimeRange, request?.Limit ?? 100);

        if (!result.Success)
            return StatusCode(500, new { message = result.Message, rows = result.Rows, columns = result.Columns, rowCount = result.RowCount, executionTimeMs = result.ExecutionTimeMs });

        return Ok(new
        {
            rows = result.Rows,
            columns = result.Columns,
            rowCount = result.RowCount,
            executionTimeMs = result.ExecutionTimeMs
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
