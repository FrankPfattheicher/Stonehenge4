using System.Text.Json.Serialization;

namespace IctBaden.Stonehenge4.ChartsC3;

public class ChartCategoryTimeseriesAxis : ChartAxis
{
    [JsonPropertyName("type")] public string Type { get; }

    [JsonIgnore] public string Format { get; }

    [JsonPropertyName("tick")] public Dictionary<string, object>? Tick { get; }

    [JsonIgnore] public string[] Values { get; }


    public ChartCategoryTimeseriesAxis(string format, DateTime[] values)
        : base("x")
    {
        // timeseries, category, indexed
        Type = "timeseries";
        Format = format;
        // explicit values (category)
        // Tick = new Dictionary<string, object>
        // {
        //     ["values"] = values.Select(t => t.ToString("t")).ToArray()
        // };
        Values = values.Select(t => t.ToString("t")).ToArray();
    }
}