using System;
using IctBaden.Stonehenge.Types;
using IctBaden.Stonehenge.ViewModel;

namespace IctBaden.Stonehenge.Vue.SampleCore.ViewModels;

public class ComponentDlg : StonehengeComponent
{
    public bool Visible { get; private set; }
    public string Caption { get; private set; }
    
    public string ComponentVar { get; set; } = string.Empty;

    public Action<bool>? Closed; 
    
    public ComponentDlg(string caption)
    {
        SupportsEvents = false;
        Caption = caption;
    }
    
    public void Show()
    {
        Visible = true;
        NotifyPropertyChanged(nameof(ComponentDlg));
    }

    public override void OnLoad()
    {
        ComponentVar = "component";
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