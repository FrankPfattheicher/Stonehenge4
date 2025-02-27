using System.Drawing;
using System.Globalization;
using IctBaden.Stonehenge.Types;

// ReSharper disable UnusedAutoPropertyAccessor.Global

// ReSharper disable FieldCanBeMadeReadOnly.Global

// ReSharper disable UnusedMember.Global

// ReSharper disable MemberCanBePrivate.Global

namespace IctBaden.Stonehenge.Extension;

public class Chart
{
    /// <summary>
    /// Id of chart element
    /// </summary>
    public string Id { get; private set; } = Element.NewId();

    /// <summary>
    /// Show series points
    /// </summary>
    public bool ShowPoints = true;

    /// <summary>
    /// Enable zooming of chart
    /// </summary>
    public bool EnableZoom = false;

    /// <summary>
    /// Define the chart's category axis
    /// </summary>
    public ChartCategoryTimeSeriesAxis? CategoryAxis = null;

    /// <summary>
    /// Define the chart's values axes (maximum two)
    /// </summary>
    public ChartValueAxis[] ValueAxes = [new ChartValueAxis(ValueAxisId.y)];

    /// <summary>
    /// The chart's data series
    /// </summary>
    public ChartSeries[] Series = [];

    /// <summary>
    /// Define chart's additionally grid lines
    /// </summary>
    public ChartGridLine[] GridLines = [];

    /// <summary>
    /// Define chart's additionally axes and series regions
    /// </summary>
    public ChartDataRegion[] DataRegions = [];

    public ChartTimeSeriesRegion[] TimeSeriesRegions = []; 

    /// <summary>
    /// Define chart's title
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public ChartTitle? Title { get; set; }

    /// <summary>
    /// Sort order of chart series tooltip list
    /// </summary>
    public ChartSortOrder SortSeriesTooltips { get; set; } = ChartSortOrder.DescendValue;

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
                var colData = new List<object?> { serie.Label };
                colData.AddRange(serie.Data);
                columns.Add(colData.ToArray());
            }

            return columns.ToArray();
        }
    }

    private object[][] Groups
    {
        get
        {
            var groupNames = Series
                .Select(s => s.Group)
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct(StringComparer.Ordinal);

            var groups = groupNames
                .Select(n =>
                    Series.Where(s => string.Equals(s.Group, n, StringComparison.Ordinal)).Select(s => s.Label)
                        .Cast<object>().ToArray());

            return groups.ToArray();
        }
    }

    private object Colors
    {
        get
        {
            var colors = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var series in Series.Where(s => s.Color != Color.Transparent))
            {
                var c = series.Color;
                colors.Add(series.Label, $"#{c.R:X2}{c.G:X2}{c.B:X2}");
            }

            return colors;
        }
    }

    private object Types
    {
        get
        {
            var types = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var series in Series.Where(s => s.Type != ChartDataType.Line))
            {
                types.Add(series.Label, series.Type.ToString().ToLower(CultureInfo.InvariantCulture));
            }

            return types;
        }
    }

    public object[] XRegions
    {
        get
        {
            var dataRegions = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var region in DataRegions.GroupBy(r => r.Series, StringComparer.Ordinal))
            {
                if (string.IsNullOrEmpty(region.Key)) continue;
            
                var regionSpec = new List<object>();
                foreach (var dataRegion in region)
                {
                    var dataSpec = new Dictionary<string, object>(StringComparer.Ordinal);
                    if (dataRegion.StartValue != null) dataSpec.Add("start", dataRegion.StartValue);
                    if (dataRegion.EndValue != null) dataSpec.Add("end", dataRegion.EndValue);
                    if (dataRegion.Style != null) dataSpec.Add("style", dataRegion.Style);
                    regionSpec.Add(dataSpec);
                }
            
                dataRegions.Add(region.Key, regionSpec.ToArray());
            }

            var tsRegions = new List<Dictionary<string, object>>();
            foreach (var region in TimeSeriesRegions)
            {
                var tsRegion = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    { "start", region.StartValue },
                    { "end", region.EndValue }
                };
                if (region.Class != null)
                    tsRegion.Add("class", region.Class);
                tsRegions.Add(tsRegion);
            }

            // ReSharper disable once CoVariantArrayConversion
            return tsRegions.ToArray();
        }
    }

public IDictionary<string, object> Point => new Dictionary<string, object>(StringComparer.Ordinal)
{
    { "show", ShowPoints }
};
public IDictionary<string, object> Zoom => new Dictionary<string, object>(StringComparer.Ordinal)
{
    { "enabled", EnableZoom }
};
public IDictionary<string, object> Axis
{
get
{
    var axis = new Dictionary<string, object>(StringComparer.Ordinal);
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
public IDictionary<string, Dictionary<string, object>> Grid
{
get
{
    var gridLines = new Dictionary<string, Dictionary<string, object>>(StringComparer.Ordinal);
    var options = new Dictionary<string, object>(StringComparer.Ordinal)
    {
        { "front", false }
    };
    gridLines.Add("lines", options);

    var lines = new Dictionary<string, object>(StringComparer.Ordinal)
    {
        { "lines", GridLines.ToArray() }
    };
    gridLines.Add(nameof(ValueAxisId.y), lines);

    return gridLines;
}
}
/// <summary>
/// Use column name as key, axis id as object.
/// By default all columns are mapped to axis 'y'
/// </summary>
public IDictionary<string, object> Axes
{
get
{
    var axes = new Dictionary<string, object>(StringComparer.Ordinal);
    foreach (var series in Series)
    {
        axes[series.Label] = series.ValueAxis.ToString();
    }

    return axes;
}
}
public IDictionary<string, object> Data
{
get
{
    var data = new Dictionary<string, object>(StringComparer.Ordinal);
    if (CategoryAxis != null)
    {
        data[CategoryAxis.Id] = CategoryAxis.Id;
    }

    //data["regions"] = Regions;
    data["axes"] = Axes;
    data["columns"] = Columns;
    data["groups"] = Groups;
    data["colors"] = Colors;
    data["types"] = Types;

    return data;
}
}
public void UpdateId() => Id = Element.NewId();

public void SetSeriesData(string series, object?[] data)
{
    var chartSeries = Series.FirstOrDefault(s => string.Equals(s.Label, series, StringComparison.Ordinal));
    if (chartSeries != null)
    {
        chartSeries.Data = data;
    }
}

}