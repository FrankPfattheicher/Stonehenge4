using System;
using System.Drawing;
using System.Linq;
using IctBaden.Framework.Types;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Extension;
using IctBaden.Stonehenge.Extension.Sankey;
using IctBaden.Stonehenge.ViewModel;

// ReSharper disable ReplaceAutoPropertyWithComputedProperty

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedMember.Global

namespace IctBaden.Stonehenge.Vue.SampleCore.ViewModels;

// ReSharper disable once UnusedType.Global
public class Charts2Vm : ActiveViewModel
{
    public int RangeMin { get; } = 0;
    public int RangeMax { get; } = 100;

    public bool FullTimeRange { get; set; } = true;
    public int SortSeriesTooltips { get; set; } = 0;

    public Chart? LineChart { get; private set; }
    public SankeyChart? SankeyChart { get; private set; }

    [SessionVariable] public int Speed { get; private set; } = 500;
    private int _start;

    public Charts2Vm(AppSession session) : base(session)
    {
    }

    public override void OnLoad()
    {
        CreateLineChart();
        SankeyChart = new SankeyChart
        {
            Nodes =
            [
                new("Alice"),
                new("Bert"),
                new("Bob") { Color = Color.Coral },
                new("Carol"),
                new("Doris")
            ],
            Links =
            [
                new("Alice", "Bob") { Value = 10 },
                new("Bert", "Bob") { Value = 5 },
                new("Bob", "Carol") { Value = 95 },
                new("Bob", "Doris") { Value = 5 }
            ]
        };
        foreach (var link in SankeyChart.Links)
        {
            link.Tooltip = $"{link.Source} -> {link.Target}\n{link.Value} units";
        }

        UpdateData();

        SetUpdateTimer(Speed);
    }

    private void
        CreateLineChart()
    {
        var time = DateTime.Now;
        var timeStamps = Enumerable.Range(0, 60)
            .Select(t => time + TimeSpan.FromMinutes(t))
            .ToArray();

        var timeSeriesAxis = new ChartCategoryTimeSeriesAxis("%H:%M", 50, timeStamps);

        LineChart = new Chart
        {
            Title = new ChartTitle("Test"),
            Series = [new ChartSeries("Sinus"), new ChartSeries("Half")],
            CategoryAxis = timeSeriesAxis,
            EnableZoom = true,
            SortSeriesTooltips = new ValidatedEnum<ChartSortOrder>(SortSeriesTooltips).Enumeration
        };
    }

    private void UpdateData()
    {
        if (SankeyChart == null || LineChart == null) return;

        var dataSinus = new object?[60];
        var dataHalf = new object?[60];
        for (var ix = 0; ix < 60; ix++)
        {
            dataSinus[ix] = FullTimeRange || ix >= 30
                ? (int)(Math.Sin((ix * 2 + _start) * Math.PI / 36) * 40) + 50
                : null;
            dataHalf[ix] = FullTimeRange || ix >= 30
                ? 50
                : null;
        }

        SankeyChart.Links[0].Value = (int)(Math.Sin((50 * 2 + _start) * Math.PI / 36) * 40) + 50;
        SankeyChart.Links[1].Value = 100 - SankeyChart.Links[0].Value;

        _start++;

        LineChart.SetSeriesData("Sinus", dataSinus);
        LineChart.SetSeriesData("Half", dataHalf);
    }

    public override void OnUpdateTimer()
    {
        UpdateData();

        NotifyPropertiesChanged([ nameof(LineChart), nameof(SankeyChart) ]);
        Session.UpdatePropertiesImmediately();
    }


    [ActionMethod]
    public void ChangeFullTimeRange()
    {
    }

    [ActionMethod]
    public void ChangeSortSeriesTooltips()
    {
        CreateLineChart();
    }

    [ActionMethod]
    public void ToggleSpeed()
    {
        Speed = 600 - Speed;

        SetUpdateTimer(Speed);
    }

    public override void OnWindowResized(int width, int height)
    {
        LineChart?.Regenerate();
    }
}