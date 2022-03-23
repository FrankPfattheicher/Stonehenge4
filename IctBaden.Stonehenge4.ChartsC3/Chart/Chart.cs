using System.Text.Json.Serialization;
using IctBaden.Stonehenge4.ChartsC3;
// ReSharper disable UnusedMember.Global

// ReSharper disable MemberCanBePrivate.Global

namespace IctBaden.Stonehenge.Extension;

public class Chart
{
    /// <summary>
    /// Chart width in pixel
    /// </summary>
    public int? Width = null;
    /// <summary>
    /// Chart height in pixel
    /// </summary>
    public int? Height = null;
    /// <summary>
    /// Show series points
    /// </summary>
    public bool ShowPoints = true;
    /// <summary>
    /// Define the chart's category axis
    /// </summary>
    public ChartCategoryTimeseriesAxis? CategoryAxis = null;
    /// <summary>
    /// Define the chart's values axes (maximum two)
    /// </summary>
    public ChartValueAxis[] ValueAxes;
    /// <summary>
    /// The chart's data series
    /// </summary>
    public ChartSeries[] Series;
    /// <summary>
    /// Define chart's additionally grid lines
    /// </summary>
    public ChartGridLine[] GridLines;
    /// <summary>
    /// Define chart's title
    /// </summary>
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
    
    public Dictionary<string, object> Size
    {
        get
        {
            var size = new Dictionary<string, object>();
            if (Width != null) size["width"] = Width;
            if (Height != null) size["height"] = Height;
            return size; 
        }
    }

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

    public Dictionary<string, Dictionary<string, object>> Grid
    {
        get
        {
            var gridLines = new Dictionary<string, Dictionary<string, object>>();
            foreach (var chartGridLines in GridLines.GroupBy(g => g.Axis))
            {
                var lines = new Dictionary<string, object>
                {
                    { "lines", chartGridLines.ToArray() }
                };
                gridLines.Add(chartGridLines.Key, lines);
            }
            return gridLines;
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
        GridLines = Array.Empty<ChartGridLine>();
    }

    public void SetSeriesData(string series, object[] data)
    {
        var serie = Series.FirstOrDefault(s => s.Label == series);
        if (serie != null) serie.Data = data;
    }
}