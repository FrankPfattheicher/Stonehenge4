using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.ViewModel;
// ReSharper disable UnusedMember.Global

namespace IctBaden.Stonehenge.Vue.SampleCore.ViewModels;

// ReSharper disable once UnusedType.Global
public class CookieVm(AppSession session) : ActiveViewModel(session)
{
    public string Theme { get; set; } = string.Empty;

    public override void OnLoad()
    {
        Theme = Session.Cookies.TryGetValue("theme", out var theme)
            ? theme
            : string.Empty;
    }

    [ActionMethod]
    public void GoBack()
    {
        NavigateBack();
    }
        
}