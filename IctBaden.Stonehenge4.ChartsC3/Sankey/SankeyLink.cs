using System.Drawing;
using System.Text.Json.Serialization;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global

namespace IctBaden.Stonehenge.Extension.Sankey;

public class SankeyLink
{
    [JsonPropertyName("source")]
    public string Source { get; init; }

    [JsonPropertyName("target")]
    public string Target { get; init; }
    
    [JsonPropertyName("value")]
    public long Value { get; set; }

    [JsonIgnore]    
    public KnownColor Color { get; set; } = KnownColor.Silver;
    public string ColorRgb
    {
        get
        {
            var c = System.Drawing.Color.FromKnownColor(Color);
            return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }
    }

    public string Tooltip { get; set; } = "";

    public SankeyLink(string source, string target)
    {
        Source = source;
        Target = target;
    }

    public override string ToString()
    {
        return $"{Source} -> {Target} ({Value})";
    }
}