using IctBaden.Stonehenge4.Core;
using IctBaden.Stonehenge4.ViewModel;

namespace IctBaden.Stonehenge4.Vue.SampleCore.ViewModels
{
    public class CookieVm : ActiveViewModel
    {
        public string Theme => Session.Cookies.ContainsKey("theme")
            ? Session.Cookies["theme"]
            : string.Empty;

        public int ThemeIndex => Theme switch
        {
            "blue" => 2,
            "dark" => 1,
            _ => 0
        };

        public CookieVm(AppSession session)
            : base(session)
        {
        }

    }
}