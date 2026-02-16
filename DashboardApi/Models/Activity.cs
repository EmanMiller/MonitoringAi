namespace DashboardApi.Models;

/// <summary>
/// Activity item for recent activity feed (in-memory or DTO).
/// </summary>
public class Activity
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty; // "dashboard_created", "query_run", "confluence_updated"
    public string Description { get; set; } = string.Empty; // e.g. "Dashboard 'Sales Q3' updated 2 hours ago"
    public DateTime Timestamp { get; set; }
    public string? UserId { get; set; }
}
