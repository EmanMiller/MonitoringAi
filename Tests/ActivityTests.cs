using NUnit.Framework;
using DashboardApi.Services;

namespace Tests;

[TestFixture]
public class ActivityTests
{
    [Test]
    public void FormatTimeAgo_JustNow_ReturnsJustNow()
    {
        var now = DateTime.UtcNow;
        var result = ActivityService.FormatTimeAgo(now.AddSeconds(-30));
        Assert.That(result, Is.EqualTo("just now"));
    }

    [Test]
    public void FormatTimeAgo_MinutesAgo_ReturnsMinutes()
    {
        var now = DateTime.UtcNow;
        var result = ActivityService.FormatTimeAgo(now.AddMinutes(-5));
        Assert.That(result, Does.Contain("minute"));
        Assert.That(result, Does.Contain("ago"));
    }

    [Test]
    public void FormatTimeAgo_HoursAgo_ReturnsHours()
    {
        var now = DateTime.UtcNow;
        var result = ActivityService.FormatTimeAgo(now.AddHours(-2));
        Assert.That(result, Does.Contain("hour"));
        Assert.That(result, Does.Contain("ago"));
    }

    [Test]
    public void FormatTimeAgo_DaysAgo_ReturnsDays()
    {
        var now = DateTime.UtcNow;
        var result = ActivityService.FormatTimeAgo(now.AddDays(-3));
        Assert.That(result, Does.Contain("day"));
        Assert.That(result, Does.Contain("ago"));
    }
}
