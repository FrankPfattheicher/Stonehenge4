using System.Drawing;
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace IctBaden.Stonehenge.Extension;

public class ChartSeries
{
    public string Label { get; set; }
    public string Group { get; init; } = string.Empty;
    
    public ChartDataType Type { get; init; }
    
    public ValueAxisId ValueAxis { get; set; }

    public Color Color { get; set; }
    
    public object?[] Data { get; set; }

    /// <summary>
    /// Format used to display labels (d3.format specifier)
    /// see https://d3js.org/d3-format
    /// ".0%"   rounded percentage
    /// "$.2f"  localized fixed-point currency
    /// "+20"   space-filled and signed
    /// ".^20"  dot-filled and centered
    /// ".2s"   SI-prefix with two significant digits
    /// "#x"    prefixed lowercase hexadecimal
    /// ",.2r"  grouped thousands with two significant digits
    /// </summary>
    public string? Format { get; set; }

    public ChartSeries(string label)
    {
        Label = label;
        Type = ChartDataType.Line;
        ValueAxis = ValueAxisId.y;
        Color = Color.Transparent;
        Data = [0];
    }
}