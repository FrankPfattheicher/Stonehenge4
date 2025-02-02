using System.Collections.Generic;

namespace IctBaden.Stonehenge.Resources;

public class ViewModelInfo(string route, string name)
{
    // CustomComponent
    public string ElementName { get; set; } = string.Empty;
    public IList<string> Bindings { get; set; } = [];

    // ViewModel
    public string Route { get; set; } = route;
    public string VmName { get; set; } = name;
    public string Title { get; set; } = string.Empty;
    public int SortIndex { get; set; } = 1; // ensure visible
    public bool Visible { get; set; }
    public IList<string> I18Names { get; set; } = [];
    
    public override string ToString()
    {
        return $"{SortIndex}: {VmName} - Vis={Visible}";
    }

}