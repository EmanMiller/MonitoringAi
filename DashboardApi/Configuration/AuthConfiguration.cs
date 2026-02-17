using System.Text;
using DashboardApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace DashboardApi.Configuration;

public static class AuthConfiguration
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
    {
        // Production: prefer env vars (JWT_SECRET or Jwt__Secret). Never use appsettings placeholder.
        var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
            ?? Environment.GetEnvironmentVariable("Jwt__Secret")
            ?? configuration["Jwt:Secret"]
            ?? "BarebonesDevSecretMin32CharsChangeInProduction";

        if (env.IsProduction())
        {
            var placeholders = new[] {
                "REPLACE_WITH_STRONG_SECRET_MIN_32_CHARS",
                "BarebonesDevSecretMin32CharsChangeInProduction",
                "DEV_SECRET_MIN_32_CHARS_CHANGE_IN_PRODUCTION"
            };
            if (string.IsNullOrEmpty(jwtSecret) || jwtSecret.Length < 32 || placeholders.Contains(jwtSecret))
                throw new InvalidOperationException(
                    "JWT Secret must be set in Production. Set JWT_SECRET or Jwt__Secret environment variable (min 32 chars). Never deploy with placeholder. See SETUP.md.");
        }
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ValidIssuer = configuration["Jwt:Issuer"] ?? "SumoLogic.DashboardApi",
                    ValidAudience = configuration["Jwt:Audience"] ?? "SumoLogic.DashboardFrontend",
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });
        services.AddAuthorization();
        return services;
    }
}
