using System;
using System.Threading;
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
    public class GraphVm : ActiveViewModel, IDisposable
    {
        public int RangeMin { get; } = 0;
        public int RangeMax { get; } = 100;

        public Chart LineChart { get; private set; }

        public int Speed { get; private set; }
        private Timer _timer;
        private int _start;

        public GraphVm(AppSession session) : base(session)
        {
            Speed = 500;
        }

        public override void OnLoad()
        {
            LineChart = new Chart()
            {
                Title = new ChartTitle("Test"),
                Series = new[] { new ChartSeries("Sinus") }
            };
            UpdateData();
            
            _timer = new Timer(_ => UpdateGraph(), this, Speed, Speed);
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private void UpdateData()
        {
            var data = new object[50];
            for (var ix = 0; ix < 50; ix++)
            {
                data[ix] = (int)(Math.Sin((ix * 2 + _start) * Math.PI / 36) * 40) + 50;
            }
            _start++;

            LineChart.SetSeriesData("Sinus", data);
        }
        
        private void UpdateGraph()
        {
            UpdateData();
            Session.UpdatePropertyImmediately(nameof(LineChart));
        }


        [ActionMethod]
        public void ToggleSpeed()
        {
            _timer.Dispose();
            Speed = 600 - Speed;
            _timer = new Timer(_ => UpdateGraph(), this, Speed, Speed);
        }

    }
}