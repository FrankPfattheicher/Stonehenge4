using System;
using System.Linq;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Extension;
using IctBaden.Stonehenge.ViewModel;
using DateTimeOffset = System.DateTimeOffset;

// ReSharper disable UnusedMember.Global

// ReSharper disable MemberCanBePrivate.Global

namespace IctBaden.Stonehenge.Vue.SampleCore.ViewModels;

public class Charts3Vm : ActiveViewModel
{
    private readonly DateTime[] _timeStamps;
    public Chart? LineChart { get; private set; }

    public Charts3Vm(AppSession session) : base(session)
    {
        var time = DateTime.Now;
        _timeStamps = Enumerable.Range(0, 60)
            .Select(t => time + TimeSpan.FromMinutes(t))
            .ToArray();
    }

    public override void OnLoad()
    {
        CreateLineChart();
    }

    private void CreateLineChart()
    {
        var timeSeriesAxis = new ChartCategoryTimeSeriesAxis("%H:%M", 50, _timeStamps);

        LineChart = new Chart
        {
            Title = new ChartTitle("Test"),
            Series = [new ChartSeries("Sinus")],
            CategoryAxis = timeSeriesAxis
        };

        var dataSinus = new object?[60];
        for (var ix = 0; ix < 60; ix++)
        {
            dataSinus[ix] = (int)(Math.Sin((ix * 2) * Math.PI / 36) * 40) + 50;
        }

        LineChart.SetSeriesData("Sinus", dataSinus);
    }

    // ====== Regions ======
    [ActionMethod]
    public void AddTimeSeriesRegions()
    {
        if (LineChart == null) return;

        LineChart.TimeSeriesRegions =
        [
            new ChartTimeSeriesRegion(
                    new DateTimeOffset(_timeStamps[9]).ToUnixTimeMilliseconds(),
                    new DateTimeOffset(_timeStamps[18]).ToUnixTimeMilliseconds())
                { Class = "chart-pos" },
            new ChartTimeSeriesRegion(
                    new DateTimeOffset(_timeStamps[27]).ToUnixTimeMilliseconds(),
                    new DateTimeOffset(_timeStamps[36]).ToUnixTimeMilliseconds())
                { Class = "chart-neg" }
        ];
    }

    [ActionMethod]
    public void RemoveTimeSeriesRegions()
    {
        if (LineChart == null) return;

        LineChart.TimeSeriesRegions = [];
    }

    [ActionMethod]
    public void AddDataSeriesRegions()
    {
        if (LineChart == null) return;

        LineChart.DataRegions =
        [
            new ChartDataSeriesRegion(ValueAxisId.y, 50, 90) { Class = "chart-pos" },
            new ChartDataSeriesRegion(ValueAxisId.y, 10, 50) { Class = "chart-neg" }
        ];
    }

    [ActionMethod]
    public void RemoveDataSeriesRegions()
    {
        if (LineChart == null) return;

        LineChart.DataRegions = [];
    }

    // ====== Lines ======
    [ActionMethod]
    public void AddTimeSeriesLines()
    {
        if (LineChart == null) return;

        RemoveTimeSeriesLines();
        LineChart.GridLines = LineChart.GridLines
            .Concat(
            [
                new ChartGridLine
                {
                    Axis = AxisId.x, Class = "chart-pos",
                    Value = new DateTimeOffset(_timeStamps[20]).ToUnixTimeMilliseconds(),
                    Text = "pos 20"
                },
                new ChartGridLine
                {
                    Axis = AxisId.x, Class = "chart-neg",
                    Value = new DateTimeOffset(_timeStamps[40]).ToUnixTimeMilliseconds(),
                    Text = "pos 40"
                }
            ]).ToArray();
        LineChart.UpdateId();
    }

    [ActionMethod]
    public void RemoveTimeSeriesLines()
    {
        if (LineChart == null) return;

        LineChart.GridLines = LineChart.GridLines
            .Where(gl => gl.Axis != AxisId.x)
            .ToArray();
        LineChart.UpdateId();
    }

    [ActionMethod]
    public void AddDataSeriesLines()
    {
        if (LineChart == null) return;

        RemoveDataSeriesLines();
        LineChart.GridLines = LineChart.GridLines
            .Concat(
            [
                new ChartGridLine
                {
                    Axis = AxisId.y, Class = "chart-pos",
                    Value = 75,
                    Text = "pos 75"
                },
                new ChartGridLine
                {
                    Axis = AxisId.y, Class = "chart-neg",
                    Value = 25,
                    Text = "pos 25"
                }
            ]).ToArray();
        LineChart.UpdateId();
    }

    [ActionMethod]
    public void RemoveDataSeriesLines()
    {
        if (LineChart == null) return;

        LineChart.GridLines = LineChart.GridLines
            .Where(gl => gl.Axis == AxisId.x)
            .ToArray();
        LineChart.UpdateId();
    }
}