using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace DashboardApi.Services;

/// <summary>Executes Sumo Logic queries via the Search Job API.</summary>
public class SumoLogicQueryService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public SumoLogicQueryService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    /// <summary>Parse time range string (e.g. "1h", "24h", "7d") to from/to in ISO 8601.</summary>
    public static (string From, string To) ParseTimeRange(string? timeRange)
    {
        var now = DateTime.UtcNow;
        var span = TimeSpan.FromHours(1);
        var trimmed = (timeRange ?? "1h").Trim().ToLowerInvariant();

        if (trimmed.EndsWith("h"))
        {
            if (int.TryParse(trimmed.TrimEnd('h'), out var h) && h > 0)
                span = TimeSpan.FromHours(Math.Min(h, 24 * 7));
        }
        else if (trimmed.EndsWith("d"))
        {
            if (int.TryParse(trimmed.TrimEnd('d'), out var d) && d > 0)
                span = TimeSpan.FromDays(Math.Min(d, 30));
        }
        else if (trimmed.EndsWith("m"))
        {
            if (int.TryParse(trimmed.TrimEnd('m'), out var m) && m > 0)
                span = TimeSpan.FromMinutes(Math.Min(m, 60 * 24));
        }

        var from = now - span;
        return (
            from.ToString("yyyy-MM-ddTHH:mm:ss"),
            now.ToString("yyyy-MM-ddTHH:mm:ss")
        );
    }

    /// <summary>Execute a Sumo Logic query and return rows/columns.</summary>
    public async Task<QueryExecutionResult> ExecuteQueryAsync(string query, string? timeRange, int limit, CancellationToken cancellationToken = default)
    {
        var apiUrl = _configuration["SumoLogic:ApiUrl"] ?? "https://api.sumologic.com";
        var apiKey = _configuration["SumoLogic:ApiKey"];
        var apiSecret = _configuration["SumoLogic:ApiSecret"];

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            return QueryExecutionResult.Failed("Sumo Logic credentials not configured.", 0);

        var (from, to) = ParseTimeRange(timeRange);
        var limitClamped = Math.Clamp(limit, 1, 1000);

        // Use dedicated client with cookie support (required by Search Job API)
        var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new CookieContainer()
        };
        using var client = new HttpClient(handler);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.ASCII.GetBytes($"{apiKey}:{apiSecret}")));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // 1. Create search job
            var createBody = new
            {
                query = query?.Trim() ?? "",
                from = from,
                to = to,
                timeZone = "UTC"
            };
            var createContent = new StringContent(JsonConvert.SerializeObject(createBody), Encoding.UTF8, "application/json");
            var createResponse = await client.PostAsync($"{apiUrl}/api/v1/search/jobs", createContent, cancellationToken);

            if (!createResponse.IsSuccessStatusCode)
            {
                var errBody = await createResponse.Content.ReadAsStringAsync(cancellationToken);
                return QueryExecutionResult.Failed($"Sumo Logic API error: {createResponse.StatusCode} - {errBody}", sw.ElapsedMilliseconds);
            }

            var createJson = await createResponse.Content.ReadAsStringAsync(cancellationToken);
            var jobId = JObject.Parse(createJson)?["id"]?.ToString();
            if (string.IsNullOrEmpty(jobId))
                return QueryExecutionResult.Failed("Invalid search job response.", sw.ElapsedMilliseconds);

            // 2. Poll until done (max ~2 min)
            var maxAttempts = 24;
            var delay = TimeSpan.FromSeconds(5);
            string state = "";
            bool isAggregation = false;

            for (var i = 0; i < maxAttempts; i++)
            {
                await Task.Delay(delay, cancellationToken);

                var statusResponse = await client.GetAsync($"{apiUrl}/api/v1/search/jobs/{jobId}", cancellationToken);
                if (!statusResponse.IsSuccessStatusCode)
                {
                    if (statusResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                        return QueryExecutionResult.Failed("Search job expired or not found.", sw.ElapsedMilliseconds);
                    var errBody = await statusResponse.Content.ReadAsStringAsync(cancellationToken);
                    return QueryExecutionResult.Failed($"Status check failed: {statusResponse.StatusCode} - {errBody}", sw.ElapsedMilliseconds);
                }

                var statusJson = await statusResponse.Content.ReadAsStringAsync(cancellationToken);
                var statusObj = JObject.Parse(statusJson);
                state = statusObj?["state"]?.ToString() ?? "";
                var messageCount = statusObj?["messageCount"]?.Value<int>() ?? 0;
                var recordCount = statusObj?["recordCount"]?.Value<int>() ?? 0;
                isAggregation = recordCount > 0;

                if (state == "DONE GATHERING RESULTS" || state == "DONE GATHERING HISTOGRAM" || state == "CANCELLED")
                    break;
            }

            if (state == "CANCELLED")
                return QueryExecutionResult.Failed("Search was cancelled.", sw.ElapsedMilliseconds);

            // 3. Fetch results - messages for non-aggregate, records for aggregate
            List<string> columns = new();
            List<Dictionary<string, object>> rows = new();

            if (isAggregation)
            {
                var recordsResponse = await client.GetAsync($"{apiUrl}/api/v1/search/jobs/{jobId}/records?offset=0&limit={limitClamped}", cancellationToken);
                if (recordsResponse.IsSuccessStatusCode)
                {
                    var recordsJson = await recordsResponse.Content.ReadAsStringAsync(cancellationToken);
                    var recordsObj = JObject.Parse(recordsJson);
                    var fields = recordsObj?["fields"] as JArray;
                    var records = recordsObj?["records"] as JArray;

                    if (fields != null)
                    {
                        foreach (var f in fields)
                            columns.Add(f?["name"]?.ToString() ?? "");
                    }
                    if (records != null)
                    {
                        foreach (var rec in records)
                        {
                            var map = rec?["map"] as JObject;
                            if (map == null) continue;
                            var row = new Dictionary<string, object>();
                            foreach (var p in map.Properties())
                                row[p.Name] = p.Value?.ToString() ?? "";
                            rows.Add(row);
                        }
                    }
                }
            }
            else
            {
                var messagesResponse = await client.GetAsync($"{apiUrl}/api/v1/search/jobs/{jobId}/messages?offset=0&limit={limitClamped}", cancellationToken);
                if (messagesResponse.IsSuccessStatusCode)
                {
                    var messagesJson = await messagesResponse.Content.ReadAsStringAsync(cancellationToken);
                    var messagesObj = JObject.Parse(messagesJson);
                    var fields = messagesObj?["fields"] as JArray;
                    var messages = messagesObj?["messages"] as JArray;

                    if (fields != null)
                    {
                        foreach (var f in fields)
                            columns.Add(f?["name"]?.ToString() ?? "");
                    }
                    if (messages != null)
                    {
                        foreach (var msg in messages)
                        {
                            var map = msg?["map"] as JObject;
                            if (map == null) continue;
                            var row = new Dictionary<string, object>();
                            foreach (var p in map.Properties())
                                row[p.Name] = p.Value?.ToString() ?? "";
                            rows.Add(row);
                        }
                    }
                }
            }

            sw.Stop();
            return new QueryExecutionResult
            {
                Success = true,
                Rows = rows,
                Columns = columns,
                RowCount = rows.Count,
                ExecutionTimeMs = sw.ElapsedMilliseconds,
                Message = null
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return QueryExecutionResult.Failed(ex.Message, sw.ElapsedMilliseconds);
        }
    }
}

public class QueryExecutionResult
{
    public bool Success { get; set; }
    public List<Dictionary<string, object>> Rows { get; set; } = new();
    public List<string> Columns { get; set; } = new();
    public int RowCount { get; set; }
    public long ExecutionTimeMs { get; set; }
    public string? Message { get; set; }

    public static QueryExecutionResult Failed(string message, long ms) => new()
    {
        Success = false,
        Message = message,
        ExecutionTimeMs = ms,
        Rows = new(),
        Columns = new()
    };
}
