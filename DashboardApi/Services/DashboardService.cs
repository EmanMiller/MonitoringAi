using DashboardApi.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DashboardApi.Services
{
    public class DashboardService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public DashboardService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> CreateDashboardFromWizardAsync(DashboardWizardRequest request)
        {
            var sumoLogicApiUrl = _configuration["SumoLogic:ApiUrl"];
            var sumoLogicApiKey = _configuration["SumoLogic:ApiKey"];
            var sumoLogicApiSecret = _configuration["SumoLogic:ApiSecret"];
            var folderId = _configuration["SumoLogic:FolderId"];

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", 
                System.Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{sumoLogicApiKey}:{sumoLogicApiSecret}")));

            // Base query with placeholders
            string baseQuery = @"_sourceCategory=""""{{environment}}/cloud/{{domain_prefix}}.{{domain}}*"""" ";

            // Variable Injection
            var variables = request.UseDefaults 
                ? new TemplateVariables { Timeslice = "15m", Domain = "example.com", DomainPrefix = "www", Environment = "prod" } 
                : request.Variables;

            if (variables == null) 
            {
                throw new System.ArgumentNullException(nameof(request.Variables), "Custom variables were selected but not provided.");
            }

            string finalQuery = baseQuery
                .Replace("{{environment}}", variables.Environment ?? "")
                .Replace("{{domain_prefix}}", variables.DomainPrefix ?? "")
                .Replace("{{domain}}", variables.Domain ?? "");

            // Panel Generation
            var panels = new List<Panel>();
            if (request.Panels != null)
            {
                int yPos = 0;
                if (request.Panels.GetValueOrDefault("Success Rate %", false))
                {
                    panels.Add(new Panel {
                        Key = "success-rate-panel", PanelType = "SumoSearchPanel", Title = "Success Rate %",
                        Queries = new List<Query> { new Query { QueryType = "Log", QueryText = finalQuery + "| success", QueryKey = "A" } },
                        Layout = new Layout { X = 0, Y = yPos, Width = 6, Height = 8 }
                    });
                }
                if (request.Panels.GetValueOrDefault("Error Rate %", false))
                {
                    panels.Add(new Panel {
                        Key = "error-rate-panel", PanelType = "SumoSearchPanel", Title = "Error Rate %",
                        Queries = new List<Query> { new Query { QueryType = "Log", QueryText = finalQuery + "| error", QueryKey = "A" } },
                        Layout = new Layout { X = 6, Y = yPos, Width = 6, Height = 8 }
                    });
                    yPos += 8;
                }
                if (request.Panels.GetValueOrDefault("Past 7 day trend", false))
                {
                    panels.Add(new Panel {
                        Key = "trend-panel", PanelType = "SumoSearchPanel", Title = "Past 7 Day Trend",
                        Queries = new List<Query> { new Query { QueryType = "Log", QueryText = finalQuery + "| timeslice 1d | count by _timeslice", QueryKey = "A" } },
                        Layout = new Layout { X = 0, Y = yPos, Width = 12, Height = 8 }
                    });
                }
            }


            var dashboard = new SumoLogicDashboard
            {
                Type = "DashboardV2SyncDefinition",
                Name = request.DashboardTitle,
                Description = $"Dashboard generated from wizard: {request.DashboardTitle}",
                FolderId = folderId,
                Dashboard = new Dashboard
                {
                    Title = request.DashboardTitle,
                    RefreshInterval = 300000,
                    Theme = "Dark", // Dark theme as requested
                    TimeRange = new TimeRange { Type = "BeginBoundedTimeRange", From = new From { Type = "LiteralTimeRange", RangeName = "Last 15 Minutes" } },
                    Panels = panels,
                    Layout = new DashboardLayout { Type = "Grid" }
                }
            };
            
            var json = JsonConvert.SerializeObject(dashboard, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{sumoLogicApiUrl}/v2/content/folders/{folderId}/import", content);

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdDashboard = JsonConvert.DeserializeObject<dynamic>(responseContent);
            return createdDashboard.url;
        }


        public async Task<string> CreateDashboardAsync(string dashboardName, string sourceCategory)
        {
            var sumoLogicApiUrl = _configuration["SumoLogic:ApiUrl"];
            var sumoLogicApiKey = _configuration["SumoLogic:ApiKey"];
            var sumoLogicApiSecret = _configuration["SumoLogic:ApiSecret"];
            var folderId = _configuration["SumoLogic:FolderId"];

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", 
                System.Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{sumoLogicApiKey}:{sumoLogicApiSecret}")));

            var dashboard = new SumoLogicDashboard
            {
                Type = "DashboardV2SyncDefinition",
                Name = dashboardName,
                Description = $"Dashboard for {dashboardName}",
                FolderId = folderId,
                Dashboard = new Dashboard
                {
                    Title = dashboardName,
                    Description = $"Dashboard for {dashboardName}",
                    RefreshInterval = 300000,
                    Theme = "Light",
                    TimeRange = new TimeRange
                    {
                        Type = "BeginBoundedTimeRange",
                        From = new From
                        {
                            Type = "LiteralTimeRange",
                            RangeName = "Last 15 Minutes"
                        }
                    },
                    Panels = new System.Collections.Generic.List<Panel>
                    {
                        new Panel
                        {
                            Key = "log-panel-1",
                            PanelType = "SumoSearchPanel",
                            Title = "Errors in the Last 15 Minutes",
                            Description = "Displays all error logs from the last 15 minutes.",
                            Queries = new System.Collections.Generic.List<Query>
                            {
                                new Query
                                {
                                    QueryType = "Log",
                                    QueryText = $"_sourceCategory={sourceCategory} AND error",
                                    QueryKey = "A",
                                    QueryMode = "Standard"
                                }
                            },
                            VisualSettings = new VisualSettings
                            {
                                Type = "Table",
                                Table = new Table
                                {
                                    Columns = new System.Collections.Generic.List<Column>
                                    {
                                        new Column { Field = "_timeslice", Label = "Time" },
                                        new Column { Field = "_raw", Label = "Raw Message" }
                                    }
                                }
                            },
                            Layout = new Layout { X = 0, Y = 0, Width = 12, Height = 8 }
                        }
                    }
                }
            };

            var json = JsonConvert.SerializeObject(dashboard, Formatting.Indented);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{sumoLogicApiUrl}/v1/content/folders/{folderId}/import", content);

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            // Assuming the response contains the URL of the new dashboard
            // This will likely need to be adjusted based on the actual API response
            var createdDashboard = JsonConvert.DeserializeObject<dynamic>(responseContent);
            return createdDashboard.url;
        }
    }
}
