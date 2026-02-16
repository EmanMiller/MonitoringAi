using DashboardApi.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
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

        public async Task UpdatePageAsync(string pageId, string dashboardUrl, string dashboardName, string projectName)
        {
            var confluenceApiUrl = _configuration["Confluence:ApiUrl"];
            var confluenceUsername = _configuration["Confluence:Username"];
            var confluenceApiToken = _configuration["Confluence:ApiToken"];

            var authenticationString = $"{confluenceUsername}:{confluenceApiToken}";
            var base64EncodedAuthenticationString = System.Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(authenticationString));

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);

            // 1. Get current page content
            var getResponse = await _httpClient.GetAsync($"{confluenceApiUrl}/rest/api/content/{pageId}?expand=body.storage,version");
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

            var putResponse = await _httpClient.PutAsync($"{confluenceApiUrl}/rest/api/content/{pageId}", updateContent);
            putResponse.EnsureSuccessStatusCode();
        }
    }
}
