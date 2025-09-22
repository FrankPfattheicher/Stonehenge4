using System.Text.Json.Serialization;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace IctBaden.Stonehenge.Extension;

public class ChartValueAxis : ChartAxis
{
    [JsonPropertyName("label")]
    public string Label { get; set; }
    
    [JsonPropertyName("show")]
    public bool Show { get; set; }
    
    [JsonPropertyName("min")]
    public double Min { get; set; }
    [JsonPropertyName("center")]
    public double? Center { get; set; }
    
    [JsonPropertyName("max")]
    public double Max { get; set; }
    
    [JsonPropertyName("tick")]
    [JsonInclude]
    private Dictionary<string, object>? Tick { get; set; }

    [JsonIgnore]
    public int TickCount
    {
        get => Tick == null || !Tick.TryGetValue("count", out var count) ? 0: (int)count;
        set
        {
            Tick ??= new Dictionary<string, object>(StringComparer.Ordinal);
            Tick.Add("count", value);
        }
    }
    [JsonIgnore]
    public double[] TickValues
    {
        get => Tick == null || !Tick.TryGetValue("values", out var values) ? [] : (double[])values;
        set
        {
            Tick ??= new Dictionary<string, object>(StringComparer.Ordinal);
            Tick.Add("values", value);
        }
    }


    public ChartValueAxis(ValueAxisId id)
        : base(id.ToString())
    {
        Label = string.Empty;
        Show = true;
        Min = 0;
        Max = 100;
    }
}