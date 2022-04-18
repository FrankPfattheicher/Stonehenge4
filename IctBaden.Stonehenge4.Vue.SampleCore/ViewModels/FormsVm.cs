using System.Linq;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Extension;
using IctBaden.Stonehenge.ViewModel;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedMember.Global

namespace IctBaden.Stonehenge.Vue.SampleCore.ViewModels
{
    // ReSharper disable once UnusedType.Global
    public class FormsVm : ActiveViewModel
    {
        public int Range { get; set; }
        public int RangeMin { get; } = 0;
        public int RangeMax { get; } = 40;

        public Chart TrendChart { get; }

        public bool ShowCookies { get; private set; }

        public FormsVm(AppSession session) : base(session)
        {
            Range = 20;

            TrendChart = new Chart()
            {
                ValueAxes = new[] { new ChartValueAxis(ValueAxisId.y) { Label = "°C", Min = 0, Max = 40 } },
                Series = new[] { new ChartSeries("Temperature") }
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
        }

        [ActionMethod]
        public void ToggleShowCookies()
        {
            ShowCookies = !ShowCookies;
            EnableRoute("cookie", ShowCookies);
        }
    }
}