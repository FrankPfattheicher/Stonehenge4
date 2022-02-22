namespace IctBaden.Stonehenge4.ChartsC3;

public class ChartSeries
{
    public string Label { get; set; }
    
    public int ValueAxis { get; set; }
    
    public object[] Data { get; set; }

    public ChartSeries(string label)
    {
        Label = label;
        Data = new object[] { 0 };
    }
}