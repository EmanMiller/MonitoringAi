namespace DashboardApi.Data;

/// <summary>Query library: Category, Key, Value (SumoLogic query), Tags (json array), CreatedBy, CreatedAt.</summary>
public class Query
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;   // max 200
    public string Value { get; set; } = string.Empty; // SumoLogic query text
    public string Tags { get; set; } = "[]";          // json array
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
