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

            // Panel Generation - supports both default (bool true) and custom selection (bool false + _custom key)
            var panels = new List<Panel>();
            if (request.Panels != null)
            {
                int yPos = 0;
                var p = request.Panels;

                bool GetDefault(string key) => p.TryGetValue(key, out var v) && v is bool b && b;
                string? GetCustom(string key) => p.TryGetValue($"{key}_custom", out var v) ? v?.ToString() : null;

                // Success Rate %
                if (GetDefault("Success Rate %"))
                {
                    panels.Add(new Panel {
                        Key = "success-rate-panel", PanelType = "SumoSearchPanel", Title = "Success Rate %",
                        Queries = new List<Query> { new Query { QueryType = "Log", QueryText = finalQuery + "| success", QueryKey = "A" } },
                        Layout = new Layout { X = 0, Y = yPos, Width = 6, Height = 8 }
                    });
                }
                else if (GetCustom("Success Rate %") is string s1)
                {
                    var title1 = s1; var q1 = finalQuery + "| success";
                    if (s1 == "Request Success %") q1 = finalQuery + "| parse \"status=*\" | where status >= 200 and status < 300";
                    else if (s1 == "Uptime %") q1 = finalQuery + "| count";
                    panels.Add(new Panel { Key = "success-rate-panel", PanelType = "SumoSearchPanel", Title = title1, Queries = new List<Query> { new Query { QueryType = "Log", QueryText = q1, QueryKey = "A" } }, Layout = new Layout { X = 0, Y = yPos, Width = 6, Height = 8 } });
                }

                // Error Rate %
                if (GetDefault("Error Rate %"))
                {
                    panels.Add(new Panel {
                        Key = "error-rate-panel", PanelType = "SumoSearchPanel", Title = "Error Rate %",
                        Queries = new List<Query> { new Query { QueryType = "Log", QueryText = finalQuery + "| error", QueryKey = "A" } },
                        Layout = new Layout { X = 6, Y = yPos, Width = 6, Height = 8 }
                    });
                }
                else if (GetCustom("Error Rate %") is string s2)
                {
                    var title2 = s2; var q2 = finalQuery + "| error";
                    if (s2 == "4xx/5xx Rate") q2 = finalQuery + "| parse \"status=*\" | where status >= 400";
                    else if (s2 == "Exception Count") q2 = finalQuery + "| _contentType=exception";
                    panels.Add(new Panel { Key = "error-rate-panel", PanelType = "SumoSearchPanel", Title = title2, Queries = new List<Query> { new Query { QueryType = "Log", QueryText = q2, QueryKey = "A" } }, Layout = new Layout { X = 6, Y = yPos, Width = 6, Height = 8 } });
                }
                yPos += 8;

                // Slow Queries
                if (GetDefault("Slow Queries"))
                {
                    panels.Add(new Panel {
                        Key = "slow-queries-panel", PanelType = "SumoSearchPanel", Title = "Slow Queries",
                        Queries = new List<Query> { new Query { QueryType = "Log", QueryText = finalQuery + "| parse \"duration=*\" | where duration > 1000", QueryKey = "A" } },
                        Layout = new Layout { X = 0, Y = yPos, Width = 6, Height = 8 }
                    });
                }
                else if (GetCustom("Slow Queries") is string s3)
                {
                    var title3 = s3; var q3 = finalQuery + "| parse \"duration=*\" | where duration > 1000";
                    if (s3 == "Query Response Time") q3 = finalQuery + "| parse \"response_time=*\" | where response_time > 500";
                    else if (s3 == "Database Latency") q3 = finalQuery + "| _sourceCategory=*database* | parse \"latency=*\"";
                    else if (s3 == "API Timeout Errors") q3 = finalQuery + "| parse \"timeout=*\" | where status >= 408";
                    panels.Add(new Panel { Key = "slow-queries-panel", PanelType = "SumoSearchPanel", Title = title3, Queries = new List<Query> { new Query { QueryType = "Log", QueryText = q3, QueryKey = "A" } }, Layout = new Layout { X = 0, Y = yPos, Width = 6, Height = 8 } });
                }
                yPos += 8;

                // Past 7 day trend
                if (GetDefault("Past 7 day trend"))
                {
                    panels.Add(new Panel {
                        Key = "trend-panel", PanelType = "SumoSearchPanel", Title = "Past 7 Day Trend",
                        Queries = new List<Query> { new Query { QueryType = "Log", QueryText = finalQuery + "| timeslice 1d | count by _timeslice", QueryKey = "A" } },
                        Layout = new Layout { X = 0, Y = yPos, Width = 12, Height = 8 }
                    });
                }
                else if (GetCustom("Past 7 day trend") is string s4)
                {
                    var title4 = s4; var q4 = finalQuery + "| timeslice 1d | count by _timeslice";
                    if (s4 == "Week-over-Week Change") q4 = finalQuery + "| timeslice 1d | count by _timeslice | compare timeshift 7d";
                    else if (s4 == "Rolling 7d Average") q4 = finalQuery + "| timeslice 1d | count | avg over 7";
                    panels.Add(new Panel { Key = "trend-panel", PanelType = "SumoSearchPanel", Title = title4, Queries = new List<Query> { new Query { QueryType = "Log", QueryText = q4, QueryKey = "A" } }, Layout = new Layout { X = 0, Y = yPos, Width = 12, Height = 8 } });
                }
            }

            if (panels.Count == 0)
            {
                panels.Add(new Panel
                {
                    Key = "default-panel",
                    PanelType = "SumoSearchPanel",
                    Title = "Logs",
                    Queries = new List<Query> { new Query { QueryType = "Log", QueryText = finalQuery, QueryKey = "A" } },
                    Layout = new Layout { X = 0, Y = 0, Width = 12, Height = 8 }
                });
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
            string? url = createdDashboard?.url?.ToString();
            if (string.IsNullOrEmpty(url) && createdDashboard?.id != null)
            {
                var baseUrl = (sumoLogicApiUrl ?? "").Replace("/api", "");
                url = $"{baseUrl}/app/dashboards#dashboard/{createdDashboard.id}";
            }
            return url ?? throw new System.InvalidOperationException("Sumo Logic did not return a dashboard URL.");
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
