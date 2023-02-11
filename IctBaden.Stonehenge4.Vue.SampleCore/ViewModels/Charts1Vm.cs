using System;
using System.Linq;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Extension;
using IctBaden.Stonehenge.Extension.Pie;
using IctBaden.Stonehenge.ViewModel;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedMember.Global

namespace IctBaden.Stonehenge.Vue.SampleCore.ViewModels
{
    // ReSharper disable once UnusedType.Global
    public class Charts1Vm : ActiveViewModel
    {
        public int Range { get; set; }
        public int RangeMin { get; } = 0;
        public int RangeMax { get; } = 40;

        public Chart TrendChart { get; }
        public PieChart PieChart { get; }

        public Charts1Vm(AppSession session) : base(session)
        {
            Range = 20;

            TrendChart = new Chart
            {
                ValueAxes = new[]
                {
                    new ChartValueAxis(ValueAxisId.y)
                    {
                        Label = "°C",
                        Min = 0,
                        Max = 40
                    }
                },
                Series = new[]
                {
                    new ChartSeries("Temperature")
                    {
                        Type = ChartDataType.Bar
                    }
                },
                EnableZoom = true
            };
            PieChart = new PieChart
            {
                Sectors = new PieSector[]
                {
                    new() { Label = "Wert", Value = 100 },
                    new() { Label = "Sonst", Value = 100 }
                }
            };

            TrendChart.SetSeriesData("Temperature", new object[] { 10, 12, 15, 14, 13, 20, 22, 25, Range });
        }

        [ActionMethod]
        public void RangeChanged()
        {
            var newData = TrendChart.Series.First().Data;
            newData = newData.Take(newData.Length - 1)
                .Concat(new object[] { Range })
                .ToArray();
            TrendChart.SetSeriesData("Temperature", newData);
            PieChart.Sectors[0].Value = Range;
        }
    }
}