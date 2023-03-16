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
                        Max = 70
                    }
                },
                Series = new[]
                {
                    new ChartSeries("Temperature1") { Type = ChartDataType.Bar, Group = "Temps" },
                    new ChartSeries("Temperature2") { Type = ChartDataType.Bar, Group = "Temps" }
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

            RangeChanged();
        }

        [ActionMethod]
        public void RangeChanged()
        {
            var newData = new object[] { 10, 12, 15, 14, 13, 20, 22, 25 }
                .Concat(new object[] { Range })
                .ToArray();
            TrendChart.SetSeriesData("Temperature1", newData);
            
            newData = new object[] { 13, 20, 22, 25, 10, 12, 15, 14 }
                .Concat(new object[] { 60 - Range })
                .ToArray();
            TrendChart.SetSeriesData("Temperature2", newData);
            
            PieChart.Sectors[0].Value = Range;
        }
    }
}