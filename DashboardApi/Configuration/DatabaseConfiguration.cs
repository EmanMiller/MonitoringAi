using DashboardApi.Data;
using DashboardApi.Services;
using Microsoft.EntityFrameworkCore;

namespace DashboardApi.Configuration;

public static class DatabaseConfiguration
{
    /// <summary>Detects PostgreSQL when connection string contains Host= or Database= (e.g. Host=localhost;Database=monitoringai).</summary>
    private static bool IsPostgresConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return false;
        var cs = connectionString.Trim();
        return cs.Contains("Host=", StringComparison.OrdinalIgnoreCase)
            || cs.Contains("Database=", StringComparison.OrdinalIgnoreCase)
            || cs.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
            || cs.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase);
    }

    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["DATABASE_URL"]
            ?? "Data Source=monitoring.db";

        // Support DATABASE_URL=postgresql://user:password@host:5432/dbname (e.g. from env/cloud)
        if (connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
            || connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(connectionString);
            var userInfo = uri.UserInfo.Split(':', 2);
            var user = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
            var pass = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
            var port = uri.Port > 0 ? uri.Port : 5432;
            connectionString = $"Host={uri.Host};Port={port};Database={uri.AbsolutePath.TrimStart('/')};Username={user};Password={pass};SSL Mode=Prefer;";
        }

        if (IsPostgresConnectionString(connectionString))
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.GetName().Name)));
        }
        else
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(connectionString));
        }

        return services;
    }

    public static async Task EnsureCreatedAndSeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var pending = await db.Database.GetPendingMigrationsAsync().ConfigureAwait(false);
        if (pending.Any())
            await db.Database.MigrateAsync().ConfigureAwait(false);
        else
            await db.Database.EnsureCreatedAsync().ConfigureAwait(false);

        if (!await db.Users.AnyAsync())
        {
            var defaultPassword = app.Configuration["Seed:DefaultAdminPassword"];
            if (!string.IsNullOrEmpty(defaultPassword))
            {
                db.Users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    Username = "admin",
                    Email = "admin@local",
                    PasswordHash = PasswordValidator.HashPassword(defaultPassword),
                    Role = "admin",
                    CreatedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }
        }

        DbSeeder.SeedData(db);
    }
}
