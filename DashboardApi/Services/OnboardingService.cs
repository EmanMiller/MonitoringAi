using System.Text.Json;
using DashboardApi.Data;
using Microsoft.EntityFrameworkCore;

namespace DashboardApi.Services;

/// <summary>
/// Interest-to-widget mapping for onboarding dashboard generation.
/// </summary>
public static class InterestWidgetMap
{
    public static readonly IReadOnlyDictionary<string, (string Title, string PanelType, string QueryHint)> Map = new Dictionary<string, (string, string, string)>(StringComparer.OrdinalIgnoreCase)
    {
        ["Application Performance"] = ("Response Time", "SumoSearchPanel", "_sourceCategory=* | parse \"duration=*\" | where duration > 0"),
        ["Error Tracking"] = ("Error Count", "SumoSearchPanel", "_sourceCategory=* | where _contentType=\"exception\" or status >= 400 | count"),
        ["User Activity"] = ("Active Users", "SumoSearchPanel", "_sourceCategory=* | parse \"user_id=*\" | count by user_id"),
        ["API Health"] = ("API Status", "SumoSearchPanel", "_sourceCategory=* | parse \"status=*\" | count by status"),
        ["Infrastructure"] = ("System Metrics", "SumoSearchPanel", "_sourceCategory=* | parse \"metric=* value=*\" | timeslice 5m | avg(value) by _timeslice"),
        ["Security Events"] = ("Security Alerts", "SumoSearchPanel", "_sourceCategory=*security* | count by _timeslice")
    };

    public static readonly string[] AllInterests = Map.Keys.ToArray();
    public static readonly string[] AllowedTimeRanges = ["1h", "24h", "7d", "30d"];
}

public class OnboardingService
{
    private readonly ApplicationDbContext _db;

    public OnboardingService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<UserPreferences> GetOrCreatePreferencesAsync(string userId)
    {
        var prefs = await _db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
        if (prefs != null) return prefs;

        prefs = new UserPreferences
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OnboardingCompleted = false,
            OnboardingSkipped = false,
            LastOnboardingStep = 0,
            SelectedInterestsJson = "[]",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.UserPreferences.Add(prefs);
        await _db.SaveChangesAsync();
        return prefs;
    }

    public async Task<UserPreferences> UpdateProgressAsync(string userId, int step)
    {
        var prefs = await GetOrCreatePreferencesAsync(userId);
        prefs.LastOnboardingStep = Math.Max(0, step);
        prefs.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return prefs;
    }

    public async Task<UserPreferences> CompleteAsync(string userId, IReadOnlyList<string> interests)
    {
        if (interests == null || interests.Count == 0)
            throw new ArgumentException("At least one interest must be selected.", nameof(interests));

        var prefs = await GetOrCreatePreferencesAsync(userId);
        prefs.OnboardingCompleted = true;
        prefs.OnboardingSkipped = false;
        prefs.LastOnboardingStep = 999;
        prefs.SelectedInterestsJson = JsonSerializer.Serialize(interests.ToList());
        prefs.CompletedAt = DateTime.UtcNow;
        prefs.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return prefs;
    }

    public async Task<UserPreferences> SkipAsync(string userId)
    {
        var prefs = await GetOrCreatePreferencesAsync(userId);
        prefs.OnboardingSkipped = true;
        prefs.OnboardingCompleted = false;
        prefs.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return prefs;
    }

    public async Task<Dashboard> CreateDashboardAsync(string userId, string dashboardName, IReadOnlyList<string> interests, string timeRange)
    {
        // Validation
        var (validName, nameError) = InputValidationService.ValidateDashboardName(dashboardName);
        if (!validName) throw new ArgumentException(nameError ?? "Invalid dashboard name.", nameof(dashboardName));

        if (interests == null || interests.Count == 0)
            throw new ArgumentException("At least one interest must be selected.", nameof(interests));

        if (string.IsNullOrWhiteSpace(timeRange) || !InterestWidgetMap.AllowedTimeRanges.Contains(timeRange))
            throw new ArgumentException($"Time range must be one of: {string.Join(", ", InterestWidgetMap.AllowedTimeRanges)}.", nameof(timeRange));

        var userGuid = Guid.TryParse(userId, out var g) ? g : Guid.NewGuid();

        var panels = new List<object>();
        int y = 0;
        foreach (var interest in interests.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!InterestWidgetMap.Map.TryGetValue(interest, out var widget))
                continue;

            panels.Add(new
            {
                key = $"panel-{interest.Replace(" ", "-").ToLowerInvariant()}",
                panelType = widget.PanelType,
                title = widget.Title,
                interest = interest,
                layout = new { x = 0, y = y, width = 12, height = 6 }
            });
            y += 6;
        }

        var config = new
        {
            timeRange,
            interests = interests.ToList(),
            panels,
            generatedAt = DateTime.UtcNow
        };

        var dashboard = new Dashboard
        {
            Id = Guid.NewGuid(),
            Name = InputValidationService.SanitizeDashboardName(dashboardName),
            UserId = userGuid,
            Configuration = JsonSerializer.Serialize(config),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Dashboards.Add(dashboard);
        await _db.SaveChangesAsync();
        return dashboard;
    }

    public static List<string> ParseInterests(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json);
            return list ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
