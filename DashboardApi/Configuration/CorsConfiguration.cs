using Microsoft.Extensions.Configuration;

namespace DashboardApi.Configuration;

public static class CorsConfiguration
{
    public const string PolicyName = "AllowLocalhost";

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var originsConfig = configuration["Cors:Origins"] ?? "http://localhost:5173";
        var origins = originsConfig.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (origins.Length == 0)
            origins = new[] { "http://localhost:3000", "http://localhost:5173" };

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                policy.WithOrigins(origins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });
        return services;
    }
}
