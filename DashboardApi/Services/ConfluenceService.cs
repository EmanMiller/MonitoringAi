using DashboardApi.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DashboardApi.Services
{
    public class ConfluenceService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ConfluenceService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string url)
        {
            var confluenceUsername = _configuration["Confluence:Username"];
            var confluenceApiToken = _configuration["Confluence:ApiToken"];
            var authString = $"{confluenceUsername}:{confluenceApiToken}";
            var base64 = System.Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(authString));
            var req = new HttpRequestMessage(method, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64);
            return req;
        }

        /// <summary>Verifies Confluence credentials and returns connection status.</summary>
        public async Task<(bool Connected, string Message)> CheckConfluenceConnectionAsync()
        {
            var confluenceApiUrl = _configuration["Confluence:ApiUrl"];
            var confluenceUsername = _configuration["Confluence:Username"];
            var confluenceApiToken = _configuration["Confluence:ApiToken"];

            if (string.IsNullOrEmpty(confluenceApiUrl))
                return (false, "Confluence API URL not configured (CONFLUENCE_API_URL).");
            if (string.IsNullOrEmpty(confluenceUsername) || string.IsNullOrEmpty(confluenceApiToken))
                return (false, "Confluence credentials not configured (CONFLUENCE_USERNAME, CONFLUENCE_API_TOKEN).");

            try
            {
                using var req = CreateAuthenticatedRequest(HttpMethod.Get, $"{confluenceApiUrl}/rest/api/user/current");
                var response = await _httpClient.SendAsync(req);
                if (response.IsSuccessStatusCode)
                    return (true, "Connected. Confluence credentials valid.");
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return (false, "Confluence authentication failed. Check username and API token.");
                var body = await response.Content.ReadAsStringAsync();
                return (false, $"Confluence connection failed: {response.StatusCode} - {body}");
            }
            catch (Exception ex)
            {
                return (false, $"Confluence connection error: {ex.Message}");
            }
        }

        public async Task UpdatePageAsync(string pageId, string dashboardUrl, string dashboardName, string projectName)
        {
            var confluenceApiUrl = _configuration["Confluence:ApiUrl"];
            if (string.IsNullOrEmpty(confluenceApiUrl))
                throw new InvalidOperationException("Confluence API URL not configured.");

            // 1. Get current page content
            using var getReq = CreateAuthenticatedRequest(HttpMethod.Get, $"{confluenceApiUrl}/rest/api/content/{pageId}?expand=body.storage,version");
            var getResponse = await _httpClient.SendAsync(getReq);
            getResponse.EnsureSuccessStatusCode();
            var jsonContent = await getResponse.Content.ReadAsStringAsync();
            var page = JsonConvert.DeserializeObject<ConfluencePage>(jsonContent);

            if (page == null || page.Body?.Storage?.Value == null || page.Version == null)
            {
                throw new System.Exception("Failed to retrieve or parse Confluence page content.");
            }

            // 2. Find tracking table and append new row (XHTML storage format); then increment version
            var storageValue = page.Body.Storage.Value;
            var xdoc = XDocument.Parse(storageValue);
            var table = xdoc.Descendants("table").FirstOrDefault();

            if (table != null)
            {
                var tbody = table.Element("tbody") ?? table;
                var linkCell = new XElement("td",
                    new XElement("a",
                        new XAttribute("href", dashboardUrl),
                        new XAttribute("target", "_blank"),
                        dashboardName));
                var newRow = new XElement("tr",
                    new XElement("td", projectName),
                    linkCell,
                    new XElement("td", "—"));
                tbody.Add(newRow);
            }
            else
            {
                // No table exists — create "Dashboard Tracking" section and table
                var newTable = new XElement("table",
                    new XElement("thead",
                        new XElement("tr",
                            new XElement("th", "Project"),
                            new XElement("th", "Dashboard"),
                            new XElement("th", "Status"))),
                    new XElement("tbody",
                        new XElement("tr",
                            new XElement("td", projectName),
                            new XElement("td",
                                new XElement("a",
                                    new XAttribute("href", dashboardUrl),
                                    new XAttribute("target", "_blank"),
                                    dashboardName)),
                            new XElement("td", "—"))));
                var root = xdoc.Root;
                if (root != null)
                {
                    root.Add(new XElement("h2", "Dashboard Tracking"));
                    root.Add(newTable);
                }
            }

            // 3. Update page with new content
            var updateRequest = new ConfluencePageUpdateRequest
            {
                Version = new Models.Version { Number = page.Version.Number + 1 },
                Title = page.Title,
                Body = new Body
                {
                    Storage = new Storage
                    {
                        Value = xdoc.ToString(),
                        Representation = "storage"
                    }
                }
            };

            var updateJson = JsonConvert.SerializeObject(updateRequest);
            var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");
            using var putReq = CreateAuthenticatedRequest(HttpMethod.Put, $"{confluenceApiUrl}/rest/api/content/{pageId}");
            putReq.Content = updateContent;

            var putResponse = await _httpClient.SendAsync(putReq);
            putResponse.EnsureSuccessStatusCode();
        }

        /// <summary>Search Confluence pages by text. Returns title, excerpt, url, space.</summary>
        public async Task<List<ConfluenceSearchResult>> SearchAsync(string query, int limit = 10, CancellationToken cancellationToken = default)
        {
            var confluenceApiUrl = _configuration["Confluence:ApiUrl"];
            var confluenceUsername = _configuration["Confluence:Username"];
            var confluenceApiToken = _configuration["Confluence:ApiToken"];

            if (string.IsNullOrEmpty(confluenceApiUrl) || string.IsNullOrEmpty(confluenceUsername) || string.IsNullOrEmpty(confluenceApiToken))
                return new List<ConfluenceSearchResult>();

            var trimmed = (query ?? "").Trim();
            if (string.IsNullOrEmpty(trimmed))
                return new List<ConfluenceSearchResult>();

            var limitClamped = Math.Clamp(limit, 1, 50);
            // Optional: scope to specific space(s) via CONFLUENCE_SPACE_KEY or CONFLUENCE_SPACE_KEYS (comma-separated)
            // Reduces noise for large Confluence instances; leave empty to search all spaces.
            var spaceKey = _configuration["Confluence:SpaceKey"]?.Trim();
            var spaceKeys = _configuration["Confluence:SpaceKeys"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => !string.IsNullOrEmpty(s)).ToArray();
            var spaceFilter = !string.IsNullOrEmpty(spaceKey)
                ? $"space=\"{spaceKey}\""
                : (spaceKeys?.Length > 0
                    ? $"space in ({string.Join(", ", spaceKeys.Select(s => $"\"{s}\""))})"
                    : "");
            var cqlParts = new List<string> { "type=page", $"text~\"{trimmed.Replace("\"", "\\\"")}\"" };
            if (!string.IsNullOrEmpty(spaceFilter)) cqlParts.Insert(0, spaceFilter);
            var cql = string.Join(" AND ", cqlParts);
            var encodedCql = Uri.EscapeDataString(cql);
            var url = $"{confluenceApiUrl}/rest/api/content/search?cql={encodedCql}&limit={limitClamped}";

            try
            {
                using var req = CreateAuthenticatedRequest(HttpMethod.Get, url);
                var response = await _httpClient.SendAsync(req, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                        response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                        return new List<ConfluenceSearchResult>();
                    return new List<ConfluenceSearchResult>();
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var obj = JObject.Parse(json);
                var resultsArray = obj["results"] as JArray;
                var results = new List<ConfluenceSearchResult>();
                if (resultsArray == null) return results;

                foreach (var r in resultsArray.OfType<JObject>())
                {
                    var link = r?["_links"] as JObject;
                    var webui = link?["webui"]?.ToString() ?? "";
                    var baseUrl = confluenceApiUrl?.TrimEnd('/') ?? "";
                    var fullUrl = !string.IsNullOrEmpty(webui)
                        ? (webui.StartsWith("http") ? webui : $"{baseUrl}{(webui.StartsWith("/") ? "" : "/")}{webui}")
                        : "";

                    var excerpt = r?["excerpt"]?.ToString() ?? "";
                    var title = r?["title"]?.ToString() ?? "Untitled";
                    var id = r?["id"]?.ToString() ?? "";
                    var spaceObj = r?["space"] as JObject;
                    var space = spaceObj?["key"]?.ToString() ?? spaceObj?["name"]?.ToString() ?? "";

                    results.Add(new ConfluenceSearchResult
                    {
                        Id = id,
                        Title = title,
                        Excerpt = excerpt,
                        Url = fullUrl,
                        Space = space
                    });
                }
                return results;
            }
            catch
            {
                return new List<ConfluenceSearchResult>();
            }
        }
    }

    public class ConfluenceSearchResult
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Excerpt { get; set; } = "";
        public string Url { get; set; } = "";
        public string Space { get; set; } = "";
    }
}
