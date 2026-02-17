namespace DashboardApi.Data;

/// <summary>
/// In-memory mock query library for natural-language matching (login tracking, checkout failures, email delivery, slow page loads).
/// </summary>
public static class MockQueryLibrary
{
    public const int MaxUserInputLength = 500;

    public static IReadOnlyList<MockQueryEntry> All { get; } = new List<MockQueryEntry>
    {
        new()
        {
            Id = "login-tracking",
            Category = "Login tracking",
            Name = "Login events by user and time",
            Description = "Tracks logins, authentication events, and user sessions. Counts logins per user over time slices.",
            Query = "_sourceCategory=Auth/Login * | parse \"user=*\" as user | count by user, _timeslice"
        },
        new()
        {
            Id = "checkout-failures",
            Category = "Checkout failures",
            Name = "Failed checkouts and payment errors",
            Description = "Failed checkouts, payment errors, and non-2xx status codes from commerce/checkout.",
            Query = "_sourceCategory=Commerce/Checkout status_code>=400 | count(status_code) as failures by _timeslice"
        },
        new()
        {
            Id = "email-delivery",
            Category = "Email delivery",
            Name = "Failed emails and bounce rates",
            Description = "Failed email delivery, bounces, and delivery status. Counts by recipient and time.",
            Query = "_sourceCategory=Email/Delivery delivery_status=failed | count by recipient, _timeslice"
        },
        new()
        {
            Id = "slow-page-loads",
            Category = "Slow page loads",
            Name = "High latency and slow requests",
            Description = "Slow page loads, high latency, requests over a latency threshold (e.g. 2000ms).",
            Query = "_sourceCategory=Apache/Access _latency>2000 | count as slow_requests by _timeslice"
        }
    };

    public static MockQueryEntry? GetById(string id)
    {
        return All.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));
    }
}

public class MockQueryEntry
{
    public string Id { get; set; } = "";
    public string Category { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Query { get; set; } = "";
}
