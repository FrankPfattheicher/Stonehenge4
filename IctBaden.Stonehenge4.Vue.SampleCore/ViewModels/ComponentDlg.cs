using System;
using System.Diagnostics.CodeAnalysis;
using IctBaden.Stonehenge.Forms.ViewModels;
using IctBaden.Stonehenge.Types;
using IctBaden.Stonehenge.ViewModel;

namespace IctBaden.Stonehenge.Vue.SampleCore.ViewModels;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class ComponentDlg : StonehengeComponent
{
    public bool Visible { get; private set; }
    public string Caption { get; private set; }

    // component within component
    public DropEdit ComponentVar { get; set; } = new(["component", "component2", "component3"]);

    public Action<bool>? Closed;
    
    public ComponentDlg(string caption)
    {
        SupportsEvents = false;
        Caption = caption;
        ComponentVar.OnChange += InputChanged;
    }
    
    public void Show()
    {
        Visible = true;
        NotifyPropertyChanged(nameof(ComponentDlg));
    }

    public override void OnLoad()
    {
        ComponentVar.Value = "component";
    }

    [ActionMethod]
    public void Ok()
    {
        Visible = false;
        Closed?.Invoke(true);
    }
    
    [ActionMethod]
    public void Cancel()
    {
        Visible = false;
        Closed?.Invoke(false);
    }

    [ActionMethod]
    public void InputChanged()
    {
        // should update ComponentVar
    }

}