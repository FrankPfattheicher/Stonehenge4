using IctBaden.Stonehenge.Types;
using IctBaden.Stonehenge.ViewModel;

namespace IctBaden.Stonehenge.Vue.SampleCore.ViewModels;

public class ComponentDlg : StonehengeComponent
{
    public bool Visible { get; private set; }
    public string ComponentVar { get; set; } = string.Empty;

    public ComponentDlg()
    {
        SupportsEvents = false;
    }
    
    public void Show() => Visible = true;
    
    [ActionMethod]
    public void Cancel() => Visible = false;

    public override void OnLoad()
    {
        ComponentVar = "component";
    }

    [ActionMethod]
    public void Ok()
    {
        Cancel();
    }

    [ActionMethod]
    public void InputChanged()
    {
        // should update ComponentVar
    }

}