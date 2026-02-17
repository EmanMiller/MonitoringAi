using System.Text.RegularExpressions;
using DashboardApi.Configuration;
using DashboardApi.Middleware;

// Load .env from project or repo root so GEMINI_API_KEY etc. are available
LoadEnvFile();

var builder = WebApplication.CreateBuilder(args);

// Configuration and services
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddJwtAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddSecurityServices();

var app = builder.Build();

await app.EnsureCreatedAndSeedAsync();

app.UseHttpsRedirection();
app.UseCors(CorsConfiguration.PolicyName);
app.UseMiddleware<JwtCookieMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseSecurityHeaders();
app.MapControllers();

app.Run();

static void LoadEnvFile()
{
    var dir = Directory.GetCurrentDirectory();
    var candidates = new[] { Path.Combine(dir, ".env"), Path.Combine(dir, "..", ".env"), Path.Combine(dir, "..", "..", ".env") };
    foreach (var path in candidates)
    {
        var full = Path.GetFullPath(path);
        if (!File.Exists(full)) continue;
        foreach (var line in File.ReadLines(full))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed[0] == '#') continue;
            var match = Regex.Match(trimmed, @"^([A-Za-z_][A-Za-z0-9_]*)=(.*)$");
            if (match.Success)
            {
                var key = match.Groups[1].Value;
                var value = match.Groups[2].Value.Trim();
                if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
                    value = value[1..^1].Replace("\\\"", "\"");
                // Don't let .env override with placeholder — so appsettings (e.g. real key) can be used
                if (string.Equals(key, "GEMINI_API_KEY", StringComparison.OrdinalIgnoreCase) &&
                    (string.IsNullOrEmpty(value) || value == "placeholder" || value == "EMAN_GOOGLE_API_KEY_HERE"))
                    continue;
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                    Environment.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.Process);
                // Map Sumo Logic env vars to ASP.NET config (SumoLogic:ApiKey = AccessId, ApiSecret = AccessKey)
                if (string.Equals(key, "SUMO_LOGIC_ACCESS_ID", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(value))
                    Environment.SetEnvironmentVariable("SumoLogic__ApiKey", value, EnvironmentVariableTarget.Process);
                if (string.Equals(key, "SUMO_LOGIC_ACCESS_KEY", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(value))
                    Environment.SetEnvironmentVariable("SumoLogic__ApiSecret", value, EnvironmentVariableTarget.Process);
                if (string.Equals(key, "SUMO_LOGIC_FOLDER_ID", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(value))
                    Environment.SetEnvironmentVariable("SumoLogic__FolderId", value, EnvironmentVariableTarget.Process);
                if (string.Equals(key, "SUMO_LOGIC_API_URL", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(value))
                    Environment.SetEnvironmentVariable("SumoLogic__ApiUrl", value, EnvironmentVariableTarget.Process);
                // Per-category folder overrides: SUMO_LOGIC_FOLDER_ID_BROWSE_PRODUCT, SUMO_LOGIC_FOLDER_ID_CHECKOUT, etc.
                var categoryMatch = Regex.Match(key, @"^SUMO_LOGIC_FOLDER_ID_(.+)$", RegexOptions.IgnoreCase);
                if (categoryMatch.Success && !string.IsNullOrEmpty(value))
                {
                    var slug = categoryMatch.Groups[1].Value; // e.g. BROWSE_PRODUCT, CHECKOUT
                    Environment.SetEnvironmentVariable($"SumoLogic__FolderId__{slug}", value, EnvironmentVariableTarget.Process);
                }
                // Confluence
                if (string.Equals(key, "CONFLUENCE_API_URL", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(value))
                    Environment.SetEnvironmentVariable("Confluence__ApiUrl", value, EnvironmentVariableTarget.Process);
                if (string.Equals(key, "CONFLUENCE_USERNAME", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(value))
                    Environment.SetEnvironmentVariable("Confluence__Username", value, EnvironmentVariableTarget.Process);
                if (string.Equals(key, "CONFLUENCE_API_TOKEN", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(value))
                    Environment.SetEnvironmentVariable("Confluence__ApiToken", value, EnvironmentVariableTarget.Process);
                if (string.Equals(key, "CONFLUENCE_PAGE_ID", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(value))
                    Environment.SetEnvironmentVariable("Confluence__PageId", value, EnvironmentVariableTarget.Process);
                if (string.Equals(key, "CONFLUENCE_SPACE_KEY", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(value))
                    Environment.SetEnvironmentVariable("Confluence__SpaceKey", value, EnvironmentVariableTarget.Process);
                if (string.Equals(key, "CONFLUENCE_SPACE_KEYS", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(value))
                    Environment.SetEnvironmentVariable("Confluence__SpaceKeys", value, EnvironmentVariableTarget.Process);
            }
        }
        break;
    }
}
