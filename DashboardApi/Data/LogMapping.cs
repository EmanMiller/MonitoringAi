namespace DashboardApi.Data;

public class LogMapping
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty; // "Environment" or "Intent"
    public string Key { get; set; } = string.Empty;     // user word e.g. "prod", "qa"
    public string Value { get; set; } = string.Empty;   // Sumo Logic string/query fragment
    public bool IsActive { get; set; } = true;
}
