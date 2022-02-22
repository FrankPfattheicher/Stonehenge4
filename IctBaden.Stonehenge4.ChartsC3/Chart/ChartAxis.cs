using System.Text.Json.Serialization;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace IctBaden.Stonehenge4.ChartsC3;

public class ChartAxis
{
    [JsonIgnore]
    public string Id { get; set; }

    
    [JsonPropertyName("label")]
    public string Label { get; set; }
    
    [JsonPropertyName("show")]
    public bool Show { get; set; }
    
    [JsonPropertyName("min")]
    public int? Min { get; set; }
    
    [JsonPropertyName("max")]
    public int? Max { get; set; }

    public ChartAxis(string id)
    {
        Id = id;
        Label = "";
        Show = true;
        Min = 0;
        Max = 100;
    }
}