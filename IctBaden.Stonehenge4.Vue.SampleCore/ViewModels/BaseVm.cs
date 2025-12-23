using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.ViewModel;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace IctBaden.Stonehenge.Vue.SampleCore.ViewModels;

public class BaseVm : ActiveViewModel
{
    [SessionVariable]
    public string SessionTitle { get; set; } = string.Empty;

    protected BaseVm(AppSession session) : base(session)
    {
    }
    
}