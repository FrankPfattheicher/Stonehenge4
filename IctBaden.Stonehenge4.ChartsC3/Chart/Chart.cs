using System.ComponentModel;
using System.Globalization;
using System.Text;
using IctBaden.Stonehenge.ViewModel;

namespace IctBaden.Stonehenge4.ChartsC3;

public class Chart
{
    public string Title;
    public ChartAxis CategoryAxis;
    public ChartAxis[] ValueAxes;
    public ChartSeries[] Series;

    private string Columns =>
        string.Join(", ", Series.Select(s => $"[ '{s.Label}', {string.Join(", ", s.Data.Select(d => d.ToString(CultureInfo.InvariantCulture)))} ]"));

    private readonly ActiveViewModel _vm;
    private readonly string _elementId;
    private readonly string _elementProperty;

    public Chart(ActiveViewModel vm, string elementId)
    {
        _vm = vm;
        _elementId = elementId;
        _elementProperty = "chart_" + elementId.Replace("-", "_"); 
        
        CategoryAxis = new ChartAxis();
        ValueAxes = Array.Empty<ChartAxis>();
        Series = Array.Empty<ChartSeries>();
        
        //vm.PropertyChanged += VmOnPropertyChanged;
    }

    private void VmOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Draw();
        
    }

    public void Draw()
    {
        var script = new StringBuilder();

        script.AppendLine($@"if(typeof(this.{_elementProperty}) == 'undefined') {{ this.{_elementProperty} = c3.generate({{");
        script.AppendLine($@"bindto: document.getElementById('{_elementId}'),");

        if (!string.IsNullOrEmpty(Title))
        {
            script.AppendLine($@"title: {{ text: '{Title}' }},");
        }

        script.AppendLine($@"data: {{ columns: [ {Columns} ] }},");
        
        script.AppendLine($@"}}); }} else {{");
        script.AppendLine($@"this.{_elementProperty}.load({{ columns: [ {Columns} ] }});");
        script.AppendLine($@"}}");

        _vm.ExecuteClientScript(script.ToString());
    }

 
}