namespace IctBaden.Stonehenge4.ChartsC3;

public class ChartSeries
{
    public string Label { get; set; }
    
    public ChartDataType Type { get; set; }
    
    public string ValueAxis { get; set; }
    
    public object[] Data { get; set; }

    public ChartSeries(string label)
    {
        Label = label;
        Type = ChartDataType.Line;
        ValueAxis = "y";
        Data = new object[] { 0 };
    }
}