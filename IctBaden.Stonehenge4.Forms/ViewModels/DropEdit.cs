using System.Diagnostics.CodeAnalysis;
using IctBaden.Stonehenge.Types;
using IctBaden.Stonehenge.ViewModel;
// ReSharper disable UnusedMember.Global

namespace IctBaden.Stonehenge.Forms.ViewModels;

[SuppressMessage("Design", "MA0046:Use EventHandler<T> to declare events")]
public class DropEdit : StonehengeComponent
{
    public string Value { get; set; } = string.Empty;
    public string[] Values { get; private set; } = [];
    public bool DropList { get; set; }

    public DropEdit()
    {
    }
    public DropEdit(string[] values)
    {
        Values = values;
    }
    
    public void SetValues(string[] values) => Values = values;

    public override string ToString() => Value;

    public event Action? OnChange; 
    
    [ActionMethod]
    public void ValueChanged(string newValue)
    {
        Value = newValue;
        DropList = false;
        
        OnChange?.Invoke();
    }
}