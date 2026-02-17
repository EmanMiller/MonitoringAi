using DashboardApi.Data;
using DashboardApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace DashboardApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OnboardingController : ControllerBase
{
    private readonly OnboardingService _onboardingService;

    public OnboardingController(OnboardingService onboardingService)
    {
        _onboardingService = onboardingService;
    }

    /// <summary>
    /// GET /api/onboarding/preferences/{userId} - Returns user's onboarding state, creates default if not found.
    /// </summary>
    [HttpGet("preferences/{userId}")]
    public async Task<ActionResult<OnboardingPreferencesResponse>> GetPreferences(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest(new { error = "UserId is required." });

        try
        {
            var prefs = await _onboardingService.GetOrCreatePreferencesAsync(userId);
            return Ok(ToResponse(prefs));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/onboarding/progress - Updates which step user is on. Allows resuming if interrupted.
    /// </summary>
    [HttpPost("progress")]
    public async Task<ActionResult<OnboardingPreferencesResponse>> UpdateProgress([FromBody] OnboardingProgressRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.UserId))
            return BadRequest(new { error = "UserId is required." });

        try
        {
            var prefs = await _onboardingService.UpdateProgressAsync(request.UserId, request.Step);
            return Ok(ToResponse(prefs));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/onboarding/complete - Marks onboarding as complete and saves selected interests.
    /// </summary>
    [HttpPost("complete")]
    public async Task<ActionResult<OnboardingPreferencesResponse>> Complete([FromBody] OnboardingCompleteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.UserId))
            return BadRequest(new { error = "UserId is required." });
        if (request.Interests == null || request.Interests.Count == 0)
            return BadRequest(new { error = "At least one interest must be selected." });

        try
        {
            var prefs = await _onboardingService.CompleteAsync(request.UserId, request.Interests);
            return Ok(ToResponse(prefs));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/onboarding/skip - Marks onboarding as skipped.
    /// </summary>
    [HttpPost("skip")]
    public async Task<ActionResult<OnboardingPreferencesResponse>> Skip([FromBody] OnboardingSkipRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.UserId))
            return BadRequest(new { error = "UserId is required." });

        try
        {
            var prefs = await _onboardingService.SkipAsync(request.UserId);
            return Ok(ToResponse(prefs));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/onboarding/create-dashboard - Generates dashboard config from interests, creates in DB, returns dashboard.
    /// </summary>
    [HttpPost("create-dashboard")]
    public async Task<ActionResult<DashboardResponse>> CreateDashboard([FromBody] OnboardingCreateDashboardRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.UserId))
            return BadRequest(new { error = "UserId is required." });
        if (string.IsNullOrWhiteSpace(request?.DashboardName))
            return BadRequest(new { error = "Dashboard name is required." });
        if (request.Interests == null || request.Interests.Count == 0)
            return BadRequest(new { error = "At least one interest must be selected." });
        if (string.IsNullOrWhiteSpace(request?.TimeRange))
            return BadRequest(new { error = "Time range is required." });

        var (validName, nameError) = InputValidationService.ValidateDashboardName(request.DashboardName);
        if (!validName)
            return BadRequest(new { error = nameError ?? "Invalid dashboard name." });

        if (!InterestWidgetMap.AllowedTimeRanges.Contains(request.TimeRange))
            return BadRequest(new { error = $"Time range must be one of: {string.Join(", ", InterestWidgetMap.AllowedTimeRanges)}." });

        try
        {
            var dashboard = await _onboardingService.CreateDashboardAsync(
                request.UserId,
                request.DashboardName,
                request.Interests,
                request.TimeRange);
            return Ok(new DashboardResponse
            {
                Id = dashboard.Id,
                Name = dashboard.Name,
                UserId = dashboard.UserId,
                Configuration = dashboard.Configuration,
                CreatedAt = dashboard.CreatedAt
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private static OnboardingPreferencesResponse ToResponse(UserPreferences prefs)
    {
        return new OnboardingPreferencesResponse
        {
            Id = prefs.Id,
            UserId = prefs.UserId,
            OnboardingCompleted = prefs.OnboardingCompleted,
            OnboardingSkipped = prefs.OnboardingSkipped,
            LastOnboardingStep = prefs.LastOnboardingStep,
            SelectedInterests = OnboardingService.ParseInterests(prefs.SelectedInterestsJson),
            CompletedAt = prefs.CompletedAt
        };
    }
}

// DTOs
public class OnboardingProgressRequest
{
    public string? UserId { get; set; }
    public int Step { get; set; }
}

public class OnboardingCompleteRequest
{
    public string? UserId { get; set; }
    public List<string>? Interests { get; set; }
}

public class OnboardingSkipRequest
{
    public string? UserId { get; set; }
}

public class OnboardingCreateDashboardRequest
{
    public string? UserId { get; set; }
    public string? DashboardName { get; set; }
    public List<string>? Interests { get; set; }
    public string? TimeRange { get; set; }
}

public class OnboardingPreferencesResponse
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = "";
    public bool OnboardingCompleted { get; set; }
    public bool OnboardingSkipped { get; set; }
    public int LastOnboardingStep { get; set; }
    public List<string> SelectedInterests { get; set; } = new();
    public DateTime? CompletedAt { get; set; }
}

public class DashboardResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public Guid UserId { get; set; }
    public string Configuration { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
