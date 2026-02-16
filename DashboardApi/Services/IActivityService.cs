using DashboardApi.Models;

namespace DashboardApi.Services;

public interface IActivityService
{
    void LogActivity(string type, string description, string? userId = null);
    List<Activity> GetRecentActivities(int count = 10);
}
