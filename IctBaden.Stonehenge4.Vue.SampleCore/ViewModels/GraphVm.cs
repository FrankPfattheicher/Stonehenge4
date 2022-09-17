using System;
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

namespace IctBaden.Stonehenge.Vue.SampleCore.ViewModels
{
    // ReSharper disable once UnusedType.Global
    public class GraphVm : ActiveViewModel
    {
        public int RangeMin { get; } = 0;
        public int RangeMax { get; } = 100;

        public Chart LineChart { get; private set; }
        public Sankey SankeyChart { get; private set; }

        public int Speed { get; private set; }
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
            SankeyChart = new Sankey()
            {
                Nodes = new SankeyNode[]
                {
                    new SankeyNode { Id = "Alice" },
                    new SankeyNode { Id = "Bert" },
                    new SankeyNode { Id = "Bob" },
                    new SankeyNode { Id = "Carol" }
                },
                Links = new[]
                {
                    new SankeyLink { Source = "Alice", Target = "Bob", Value = 10 },
                    new SankeyLink { Source = "Bert", Target = "Bob", Value = 5 },
                    new SankeyLink { Source = "Bob", Target = "Carol", Value = 20 }
                }
            };
            UpdateData();

            SetUpdateTimer(Speed);
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

        public override void OnUpdateTimer()
        {
            UpdateData();
            Session.UpdatePropertyImmediately(nameof(LineChart));
        }


        [ActionMethod]
        public void ToggleSpeed()
        {
            Speed = 600 - Speed;

            SetUpdateTimer(Speed);
        }
    }
}