namespace DashboardApi.Data;

public class QueryLibraryItem
{
    public Guid Id { get; set; }
    public string Category { get; set; } = "";
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public string? TagsJson { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string RoleRequired { get; set; } = "developer";
    public int UsageCount { get; set; }
}
