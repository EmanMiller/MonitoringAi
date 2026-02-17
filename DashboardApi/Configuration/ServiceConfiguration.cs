using DashboardApi.Filters;
using DashboardApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace DashboardApi.Configuration;

public static class ServiceConfiguration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddControllers(o =>
        {
            o.Filters.Add<LogPermissionDenialFilter>();
        });

        services.AddHttpClient<DashboardService>();
        services.AddHttpClient<ConfluenceService>();
        services.AddHttpClient<QueryAssistantService>();
        services.AddHttpClient<GeminiChatService>();

        services.AddScoped<DashboardService>();
        services.AddScoped<ConfluenceService>();
        services.AddScoped<OnboardingService>();
        services.AddSingleton<IActivityService, ActivityService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<LogPermissionDenialFilter>();
        services.AddSingleton<ChatRateLimitService>();
        services.AddSingleton<DashboardRateLimitService>();
        services.AddScoped<QueryAssistantAiService>();
        services.AddScoped<DashboardFlowService>();

        return services;
    }
}
