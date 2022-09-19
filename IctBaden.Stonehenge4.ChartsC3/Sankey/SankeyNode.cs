using System.Drawing;
using System.Text.Json.Serialization;

namespace IctBaden.Stonehenge.Extension.Sankey;

public class SankeyNode
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    public KnownColor Color { get; set; } = KnownColor.LightSkyBlue;

    public string ColorRgb
    {
        get
        {
            var c = System.Drawing.Color.FromKnownColor(Color);
            return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }
    }


    public string NodeStroke { get; set; } = "";

}