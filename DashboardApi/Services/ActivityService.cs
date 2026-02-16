using DashboardApi.Models;

namespace DashboardApi.Services;

public class ActivityService : IActivityService
{
    private readonly List<Activity> _activities = new();
    private readonly object _lock = new();

    public ActivityService()
    {
        // Seed data for testing / until DB is used in production
        LogActivity("dashboard_update", "Dashboard 'Sales Q3' updated 2 hours ago");
        LogActivity("query_run", "Query 'Inventory Check' ran successfully");
        LogActivity("confluence_created", "New Confluence page: 'Q4 Planning'");
    }

    public void LogActivity(string type, string description, string? userId = null)
    {
        lock (_lock)
        {
            _activities.Add(new Activity
            {
                Id = Guid.NewGuid(),
                Type = type ?? "unknown",
                Description = description ?? "",
                Timestamp = DateTime.UtcNow,
                UserId = userId
            });
        }
    }

    public List<Activity> GetRecentActivities(int count = 10)
    {
        lock (_lock)
        {
            return _activities
                .OrderByDescending(a => a.Timestamp)
                .Take(Math.Clamp(count, 1, 20))
                .ToList();
        }
    }

    /// <summary>
    /// Returns a human-readable relative time string, e.g. "2 hours ago", "5 minutes ago".
    /// </summary>
    public static string FormatTimeAgo(DateTime timestamp)
    {
        var utc = timestamp.Kind == DateTimeKind.Utc ? timestamp : DateTime.SpecifyKind(timestamp, DateTimeKind.Utc);
        var elapsed = DateTime.UtcNow - utc;

        if (elapsed.TotalSeconds < 60) return "just now";
        if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes} minute{(elapsed.TotalMinutes >= 2 ? "s" : "")} ago";
        if (elapsed.TotalHours < 24) return $"{(int)elapsed.TotalHours} hour{(elapsed.TotalHours >= 2 ? "s" : "")} ago";
        if (elapsed.TotalDays < 30) return $"{(int)elapsed.TotalDays} day{(elapsed.TotalDays >= 2 ? "s" : "")} ago";
        if (elapsed.TotalDays < 365) return $"{(int)(elapsed.TotalDays / 30)} month{(elapsed.TotalDays >= 60 ? "s" : "")} ago";
        return $"{(int)(elapsed.TotalDays / 365)} year{(elapsed.TotalDays >= 730 ? "s" : "")} ago";
    }
}
