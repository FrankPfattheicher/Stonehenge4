using System.Drawing;
using IctBaden.Stonehenge4.ChartsC3;

namespace IctBaden.Stonehenge.Extension;

public class ChartSeries
{
    public string Label { get; set; }
    
    public ChartDataType Type { get; set; }
    
    public string ValueAxis { get; set; }

    public KnownColor Color { get; set; }
    
    public object[] Data { get; set; }

    public ChartSeries(string label)
    {
        Label = label;
        Type = ChartDataType.Line;
        ValueAxis = "y";
        Color = KnownColor.Transparent;
        Data = new object[] { 0 };
    }
}