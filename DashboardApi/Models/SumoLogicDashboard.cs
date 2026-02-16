using Newtonsoft.Json;
using System.Collections.Generic;

namespace DashboardApi.Models
{
    public class SumoLogicDashboard
    {
        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("folderId")]
        public string? FolderId { get; set; }

        [JsonProperty("dashboard")]
        public Dashboard? Dashboard { get; set; }
    }

    public class Dashboard
    {
        [JsonProperty("title")]
        public string? Title { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("refreshInterval")]
        public int RefreshInterval { get; set; }

        [JsonProperty("theme")]
        public string? Theme { get; set; }

        [JsonProperty("timeRange")]
        public TimeRange? TimeRange { get; set; }

        [JsonProperty("panels")]
        public List<Panel>? Panels { get; set; }

        [JsonProperty("layout")]
        public DashboardLayout? Layout { get; set; }
    }

    public class DashboardLayout
    {
        [JsonProperty("type")]
        public string? Type { get; set; }
    }


    public class TimeRange
    {
        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("from")]
        public From? From { get; set; }
    }

    public class From
    {
        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("rangeName")]
        public string? RangeName { get; set; }
    }

    public class Panel
    {
        [JsonProperty("key")]
        public string? Key { get; set; }

        [JsonProperty("panelType")]
        public string? PanelType { get; set; }

        [JsonProperty("title")]
        public string? Title { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("queries")]
        public List<Query>? Queries { get; set; }

        [JsonProperty("visualSettings")]
        public VisualSettings? VisualSettings { get; set; }

        [JsonProperty("layout")]
        public Layout? Layout { get; set; }
    }

    public class Query
    {
        [JsonProperty("queryType")]
        public string? QueryType { get; set; }

        [JsonProperty("query")]
        public string? QueryText { get; set; }

        [JsonProperty("queryKey")]
        public string? QueryKey { get; set; }

        [JsonProperty("queryMode")]
        public string? QueryMode { get; set; }
    }

    public class VisualSettings
    {
        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("table")]
        public Table? Table { get; set; }
    }

    public class Table
    {
        [JsonProperty("columns")]
        public List<Column>? Columns { get; set; }
    }

    public class Column
    {
        [JsonProperty("field")]
        public string? Field { get; set; }

        [JsonProperty("label")]
        public string? Label { get; set; }
    }

    public class Layout
    {
        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }
    }
}