using System.Text;
using DashboardApi.Data;
using DashboardApi.Middleware;
using DashboardApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(o =>
{
    o.Filters.Add<DashboardApi.Filters.LogPermissionDenialFilter>();
});
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=monitoring.db"));
builder.Services.AddHttpClient<DashboardService>();
builder.Services.AddHttpClient<ConfluenceService>();
builder.Services.AddHttpClient<QueryAssistantService>();
builder.Services.AddHttpClient<GeminiChatService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ConfluenceService>();

var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret is required.");
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "SumoLogic.DashboardApi",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "SumoLogic.DashboardFrontend",
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(o => o.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<ChatRateLimitService>();
builder.Services.AddScoped<DashboardApi.Filters.LogPermissionDenialFilter>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowViteApp", policy =>
    {
        policy.WithOrigins(builder.Configuration["Cors:Origins"] ?? "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    if (!await db.Users.AnyAsync())
    {
        var defaultPassword = builder.Configuration["Seed:DefaultAdminPassword"];
        if (!string.IsNullOrEmpty(defaultPassword))
        {
            db.Users.Add(new User
            {
                UserName = "admin",
                PasswordHash = PasswordValidator.HashPassword(defaultPassword),
                Role = "admin"
            });
            await db.SaveChangesAsync();
        }
    }
}

app.UseHttpsRedirection();
app.UseCors("AllowViteApp");
app.UseMiddleware<LoginRateLimitMiddleware>();
app.UseMiddleware<JwtCookieMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; " +
        "connect-src 'self' http://localhost:* https://localhost:* https://generativelanguage.googleapis.com; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline';");
    await next();
});

app.MapControllers();

app.Run();
