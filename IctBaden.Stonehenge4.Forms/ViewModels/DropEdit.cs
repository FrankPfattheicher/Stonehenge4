using IctBaden.Stonehenge.Types;
using IctBaden.Stonehenge.ViewModel;
// ReSharper disable UnusedMember.Global

namespace IctBaden.Stonehenge.Forms.ViewModels;

public class DropEdit : StonehengeComponent
{
    public string Value { get; set; } = string.Empty;
    public string[] Values { get; }
    public bool DropList { get; set; }

    public DropEdit(string[] values)
    {
        Values = values;
    }
    
    public override string ToString() => Value;

    [ActionMethod]
    public void OnChange(string newValue)
    {
        Value = newValue;
        DropList = false;
    }
}