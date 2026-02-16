using System.Collections.Concurrent;

namespace DashboardApi.Services;

/// <summary>
/// Rate limit: max 20 messages per minute per user.
/// </summary>
public class ChatRateLimitService
{
    private static readonly ConcurrentDictionary<string, RateLimitEntry> Entries = new();
    private const int MaxPerMinute = 20;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public (bool Allowed, int RetryAfterSeconds) TryConsume(string userId)
    {
        var now = DateTime.UtcNow;
        var entry = Entries.AddOrUpdate(userId,
            _ => new RateLimitEntry(1, now),
            (_, e) =>
            {
                if (now - e.WindowStart > Window)
                    return new RateLimitEntry(1, now);
                return new RateLimitEntry(e.Count + 1, e.WindowStart);
            });
        if (entry.Count <= MaxPerMinute)
            return (true, 0);
        var retryAfter = (int)(entry.WindowStart.Add(Window) - now).TotalSeconds;
        return (false, Math.Max(1, retryAfter));
    }

    private class RateLimitEntry
    {
        public int Count { get; }
        public DateTime WindowStart { get; }
        public RateLimitEntry(int count, DateTime windowStart) { Count = count; WindowStart = windowStart; }
    }
}
