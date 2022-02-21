namespace IctBaden.Stonehenge4.ChartsC3;

public class ChartSeries
{
    public string Label { get; set; }
    
    public int ValueAxis { get; set; }
    
    public double[] Data { get; set; }

    public ChartSeries(string label)
    {
        Label = label;
        Data = new double[] { 0 };
    }
}