using System;
using System.Threading;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.ViewModel;
using IctBaden.Stonehenge4.ChartsC3;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedMember.Global

namespace IctBaden.Stonehenge.Vue.SampleCore.ViewModels
{
    // ReSharper disable once UnusedType.Global
    public class GraphVm : ActiveViewModel
    {
        public int RangeMin { get; } = 0;
        public int RangeMax { get; } = 100;

        public Chart LineChart;

        private int _speed;
        private Timer _timer;
        private int _start;
        
        public GraphVm(AppSession session) : base(session)
        {
            _speed = 500;
        }

        public override void OnLoad()
        {
            LineChart = new Chart(this, "line-chart")
            {
                Title = "Test"
            };
            LineChart.Draw();
            
            _timer = new Timer(_ => UpdateGraph(), this, _speed, _speed);
        }

        private void UpdateGraph()
        {
            if (LineChart == null) return;
            
            var data = new double [50];
            for (var ix = 0; ix < 50; ix++)
            {
                data[ix] = (int)(Math.Sin((ix * 2 + _start) * Math.PI / 36) * 40) + 50;
            }
            _start++;

            LineChart.Series = new[]
            {
                new ChartSeries("Sinus")
                {
                    Data = data
                }
            };
            LineChart.Draw();
            NotifyAllPropertiesChanged();
        }


        [ActionMethod]
        public void ToggleSpeed()
        {
            _timer.Dispose();
            _speed = 600 - _speed;
            _timer = new Timer(_ => UpdateGraph(), this, _speed, _speed);
        }
        
    }
}