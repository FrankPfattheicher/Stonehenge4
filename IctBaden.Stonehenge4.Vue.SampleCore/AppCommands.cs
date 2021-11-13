using IctBaden.Stonehenge4.Core;
using IctBaden.Stonehenge4.Hosting;
using IctBaden.Stonehenge4.ViewModel;

namespace IctBaden.Stonehenge4.Vue.SampleCore
{
    // ReSharper disable once UnusedMember.Global
    public class AppCommands : IStonehengeAppCommands
    {
        // ReSharper disable once UnusedMember.Global
        public void FileOpen(AppSession session)
        {
            var vm = session.ViewModel as ActiveViewModel;
            vm?.MessageBox("AppCommand", "FileOpen");
        }
    }
}
