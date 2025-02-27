namespace IctBaden.Stonehenge.Extension;

public class ChartTimeSeriesRegion
{
    public object StartValue { get; init; }
    public object EndValue { get; init; }
    public string? Class { get; init; } = null;

    public ChartTimeSeriesRegion(object start, object end)
    {
        StartValue = start;
        EndValue = end;
    }

}