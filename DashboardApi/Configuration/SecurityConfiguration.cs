namespace DashboardApi.Configuration;

public static class SecurityConfiguration
{
    public static IServiceCollection AddSecurityServices(this IServiceCollection services)
    {
        return services;
    }

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000");
            context.Response.Headers.Append("Content-Security-Policy",
                "default-src 'self'; " +
                "connect-src 'self' http://localhost:* https://localhost:* https://generativelanguage.googleapis.com; " +
                "script-src 'self' 'unsafe-inline'; " +
                "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
                "font-src 'self' https://fonts.gstatic.com;");
            await next();
        });
        return app;
    }
}
