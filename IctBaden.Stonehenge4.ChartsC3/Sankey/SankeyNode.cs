using System.Drawing;
using System.Text.Json.Serialization;
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace IctBaden.Stonehenge.Extension.Sankey;

public class SankeyNode
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";

    private string _name = "";
    [JsonPropertyName("name")] public string Name
    {
        get => string.IsNullOrEmpty(_name) ? Id : _name;
        set => _name = value;
    }

    [JsonIgnore]    
    public KnownColor Color { get; set; } = KnownColor.LightSkyBlue;
    public string ColorRgb
    {
        get
        {
            var c = System.Drawing.Color.FromKnownColor(Color);
            return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }
    }

    public string? NodeStroke { get; set; }

}