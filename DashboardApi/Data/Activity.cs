namespace DashboardApi.Data;

public class Activity
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty; // "dashboard_created", "query_run", etc.
    public string Description { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Metadata { get; set; } // optional json
}
