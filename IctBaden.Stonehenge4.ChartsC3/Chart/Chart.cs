using System.Text.Json.Serialization;

namespace IctBaden.Stonehenge4.ChartsC3;

public class Chart
{
    public bool ShowPoints = true;
    public ChartCategoryTimeseriesAxis? CategoryAxis = null;

    public ChartValueAxis[] ValueAxes;
    public ChartSeries[] Series;

    // ReSharper disable once UnusedMember.Global
    public ChartTitle? Title { get; set; }

    [JsonPropertyName("columns")]
    private object[] Columns
    {
        get
        {
            var columns = new List<object>();
            if (CategoryAxis != null)
            {
                var colData = new List<object> { CategoryAxis.Id };
                colData.AddRange(CategoryAxis.Values.Cast<object>());
                columns.Add(colData.ToArray());
            }
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
            if (CategoryAxis != null)
            {
                axis[CategoryAxis.Id] = CategoryAxis;
            }
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
            foreach (var serie in Series)
            {
                axes[serie.Label] = serie.ValueAxis;
            }

            return axes;
        }
    }

    public Dictionary<string, object> Data
    {
        get
        {
            var data = new Dictionary<string, object>();
            if (CategoryAxis != null)
            {
                data[CategoryAxis.Id] = CategoryAxis.Id;
            }
            data["axes"] = Axes;
            data["columns"] = Columns;
            return data;
        }
    }


    public Chart()
    {
        ValueAxes = new[] { new ChartValueAxis("y") };
        Series = Array.Empty<ChartSeries>();
    }

    public void SetSeriesData(string series, object[] data)
    {
        var serie = Series.FirstOrDefault(s => s.Label == series);
        if (serie != null) serie.Data = data;
    }
}