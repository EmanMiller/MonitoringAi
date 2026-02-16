using DashboardApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace DashboardApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActivityController : ControllerBase
{
    private readonly IActivityService _activityService;

    public ActivityController(IActivityService activityService)
    {
        _activityService = activityService;
    }

    /// <summary>
    /// GET /api/activity/recent - returns last 10-20 activities, newest first.
    /// </summary>
    [HttpGet("recent")]
    public IActionResult GetRecent([FromQuery] int count = 10)
    {
        var take = Math.Clamp(count, 1, 20);
        var activities = _activityService.GetRecentActivities(take);
        var items = activities.Select(a => new
        {
            id = a.Id,
            type = a.Type,
            description = a.Description,
            timestamp = a.Timestamp,
            timeAgo = ActivityService.FormatTimeAgo(a.Timestamp)
        }).ToList();

        return Ok(new { activities = items });
    }
}
