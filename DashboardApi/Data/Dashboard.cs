namespace DashboardApi.Data;

public class Dashboard
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty; // max 50
    public Guid UserId { get; set; }
    public string Configuration { get; set; } = "{}"; // json
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
