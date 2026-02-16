namespace DashboardApi.Data;

public class QueryLibraryItem
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;       // search intent description
    public string Value { get; set; } = string.Empty;     // SumoLogic query
    public string TagsJson { get; set; } = "[]";          // JSON array of strings
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string RoleRequired { get; set; } = "developer"; // minimum role to view
    public int UsageCount { get; set; }
}
