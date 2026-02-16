using Newtonsoft.Json;

namespace DashboardApi.Models
{
    // Used to represent the response from a GET request to a Confluence page
    public class ConfluencePage
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("title")]
        public string? Title { get; set; }

        [JsonProperty("body")]
        public Body? Body { get; set; }

        [JsonProperty("version")]
        public Version? Version { get; set; }
    }

    // Used in the request body for updating a Confluence page
    public class ConfluencePageUpdateRequest
    {
        [JsonProperty("version")]
        public Version? Version { get; set; }

        [JsonProperty("title")]
        public string? Title { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } = "page";

        [JsonProperty("body")]
        public Body? Body { get; set; }
    }

    public class Body
    {
        [JsonProperty("storage")]
        public Storage? Storage { get; set; }
    }

    public class Storage
    {
        [JsonProperty("value")]
        public string? Value { get; set; }

        [JsonProperty("representation")]
        public string Representation { get; set; } = "storage";
    }

    public class Version
    {
        [JsonProperty("number")]
        public int Number { get; set; }
    }
}