using System.Text.Json.Serialization;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace IctBaden.Stonehenge.Extension;

public class ChartGridLine
{
    [JsonIgnore] public string Axis { get; init; } = "y";
    [JsonPropertyName("value")] public double Value { get; init; }
    [JsonPropertyName("text")] public string Text { get; init; } = "";
}