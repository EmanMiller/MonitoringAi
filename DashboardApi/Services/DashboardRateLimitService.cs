using System.Collections.Concurrent;

namespace DashboardApi.Services;

/// <summary>
/// Rate limit dashboard creation: max 5 per 15 minutes per user/key to prevent abuse.
/// </summary>
public class DashboardRateLimitService
{
    private static readonly ConcurrentDictionary<string, RateLimitEntry> Entries = new();
    private const int MaxPerWindow = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    public (bool Allowed, int RetryAfterSeconds) TryConsume(string userIdOrKey)
    {
        var now = DateTime.UtcNow;
        var entry = Entries.AddOrUpdate(
            userIdOrKey,
            _ => new RateLimitEntry(1, now),
            (_, e) =>
            {
                if (now - e.Start > Window)
                    return new RateLimitEntry(1, now);
                return new RateLimitEntry(e.Count + 1, e.Start);
            });
        if (entry.Count <= MaxPerWindow)
            return (true, 0);
        var retryAfter = (int)(entry.Start.Add(Window) - now).TotalSeconds;
        return (false, Math.Max(1, retryAfter));
    }

    private class RateLimitEntry
    {
        public int Count { get; }
        public DateTime Start { get; }
        public RateLimitEntry(int count, DateTime start) { Count = count; Start = start; }
    }
}
