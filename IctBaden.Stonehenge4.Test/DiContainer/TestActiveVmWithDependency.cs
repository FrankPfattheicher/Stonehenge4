using IctBaden.Stonehenge4.Core;
using IctBaden.Stonehenge4.ViewModel;

namespace IctBaden.Stonehenge4.Test.DiContainer
{
    public class TestActiveVmWithDependency : ActiveViewModel
    {
        public readonly ResolveVmDependenciesTest Test;

        public TestActiveVmWithDependency(AppSession session, ResolveVmDependenciesTest test)
            : base(session)
        {
            Test = test;
        }

    }
}
