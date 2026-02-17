namespace DashboardApi.Models;

/// <summary>
/// Activity item for recent activity feed (in-memory or DTO).
/// </summary>
public class ActivityDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? UserId { get; set; }
}
