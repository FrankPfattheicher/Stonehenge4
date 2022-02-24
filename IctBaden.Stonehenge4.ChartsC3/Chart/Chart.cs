using System.Text.Json.Serialization;

namespace IctBaden.Stonehenge4.ChartsC3;

public class Chart
{
    public bool ShowPoints = true;
    private ChartAxis CategoryAxis;
    public ChartAxis[] ValueAxes;
    public ChartSeries[] Series;

    public ChartTitle? Title { get; set; }

    [JsonPropertyName("columns")]
    private object[] Columns
    {
        get
        {
            var columns = new List<object>();
            foreach (var serie in Series)
            {
                var colData = new List<object> { serie.Label };
                colData.AddRange(serie.Data);
                columns.Add(colData.ToArray());
            }
            return columns.ToArray();
        }
    }

    public Dictionary<string, object> Point => new()
    {
        { "show", ShowPoints }
    };

    public Dictionary<string, object> Axis
    {
        get
        {
            var axis = new Dictionary<string, object>();
            foreach (var ax in ValueAxes)
            {
                axis[ax.Id] = ax;
            }

            return axis;
        }
    }

    /// <summary>
    /// Use column name as key, axis id as object.
    /// By default all columns are mapped to axis 'y'
    /// </summary>
    public Dictionary<string, object> Axes
    {
        get
        {
            var axes = new Dictionary<string, object>();
            foreach (var name in Series.Select(s => s.Label))
            {
                axes[name] = "y";
            }
            return axes;
        }
    }

    public Dictionary<string, object?> Data => new Dictionary<string, object?>
    {
        ["axes"] = Axes,
        ["columns"] = Columns
    };


    public Chart()
    {
        CategoryAxis = new ChartAxis("x");
        ValueAxes = new[]
        {
            new ChartAxis("y")
        };
        Series = Array.Empty<ChartSeries>();
    }

    public void SetSeriesData(string series, object[] data)
    {
        var serie = Series.FirstOrDefault(s => s.Label == series);
        if (serie != null) serie.Data = data;
    }
    
}