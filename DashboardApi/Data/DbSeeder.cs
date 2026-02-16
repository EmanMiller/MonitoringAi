using Microsoft.EntityFrameworkCore;

namespace DashboardApi.Data;

public static class DbSeeder
{
    public static void SeedData(ApplicationDbContext context)
    {
        if (!context.Activities.Any())
        {
            context.Activities.AddRange(
                new Activity
                {
                    Id = Guid.NewGuid(),
                    Type = "dashboard_update",
                    Description = "Dashboard 'Sales Q3' updated 2 hours ago",
                    Timestamp = DateTime.UtcNow.AddHours(-2)
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    Type = "query_run",
                    Description = "Query 'Inventory Check' ran successfully",
                    Timestamp = DateTime.UtcNow.AddHours(-1)
                },
                new Activity
                {
                    Id = Guid.NewGuid(),
                    Type = "confluence_created",
                    Description = "New Confluence page: 'Q4 Planning'",
                    Timestamp = DateTime.UtcNow.AddMinutes(-30)
                }
            );
            context.SaveChanges();
        }
    }
}
