using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.ViewModel;

namespace IctBaden.Stonehenge.Vue.SampleCore.ViewModels;

public class BaseVm : ActiveViewModel
{
    [SessionVariable]
    public string SessionTitle { get; set; } = string.Empty;

    [SessionVariable]
    public string SessionTitle2 = string.Empty;

    protected BaseVm(AppSession session) : base(session)
    {
    }
}