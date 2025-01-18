using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.ViewModel;
using Microsoft.Extensions.Logging;

namespace IctBaden.Stonehenge.Vue.TestVm.ViewModels;

public class ProtectedVm : ActiveViewModel
{
    protected ProtectedVm(AppSession session, ILogger logger)
        : base(session)
    {
        logger.LogCritical("Should not be called");
    }
}