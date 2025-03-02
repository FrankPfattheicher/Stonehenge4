namespace IctBaden.Stonehenge.Extension;

// ReSharper disable once ClassNeverInstantiated.Global
public class ChartDataSeriesRegion
{
    public string Axis { get; init; }
    public object StartValue { get; init; }
    public object EndValue { get; init; }
    public string? Class { get; init; } = null;

    public ChartDataSeriesRegion(ValueAxisId axis, object start, object end)
    {
        Axis = axis.ToString();
        StartValue = start;
        EndValue = end;
    }
}