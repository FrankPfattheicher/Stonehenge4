using System.Text.Json.Serialization;

namespace IctBaden.Stonehenge.Extension.Sankey;

public class SankeyLink
{
    [JsonPropertyName("source")]
    public string Source { get; set; }

    [JsonPropertyName("target")]
    public string Target { get; set; }
    
    [JsonPropertyName("value")]
    public long Value { get; set; }

}