namespace DashboardApi.Data;

public class SavedQuery
{
    public int Id { get; set; }
    /// <summary>Display key / question (e.g. "Slow logins last 24h")</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Raw Sumo Logic query</summary>
    public string QueryText { get; set; } = string.Empty;
    /// <summary>Category for browsing: Browse Product, Browse Path, Account, Checkout, Gift Registry, API</summary>
    public string Category { get; set; } = string.Empty;
    /// <summary>Comma-separated tags for search (e.g. "login, auth, slow")</summary>
    public string Tags { get; set; } = string.Empty;
    /// <summary>Usage count for "popular" ranking</summary>
    public int UsageCount { get; set; }
}
