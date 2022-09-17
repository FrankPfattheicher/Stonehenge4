using System.Text.Json.Serialization;

namespace IctBaden.Stonehenge.Extension.Sankey;

public class SankeyNode
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}