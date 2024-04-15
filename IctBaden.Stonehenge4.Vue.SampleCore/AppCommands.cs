using System.Linq;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.ViewModel;
using IctBaden.Stonehenge.Vue.SampleCore.ViewModels;
using Microsoft.Extensions.Logging;

namespace IctBaden.Stonehenge.Vue.SampleCore;

// ReSharper disable once UnusedType.Global
public class AppCommands(ILogger logger) : IStonehengeAppCommands
{
    // ReSharper disable once UnusedMember.Global
    public void FileOpen(AppSession session)
    {
        var vm = session.ViewModel as ActiveViewModel;
        vm?.MessageBox("AppCommand", "FileOpen");
    }
        
    public void WindowResized(AppSession session, int width, int height)
    {
        var paramWidth = session.Parameters.FirstOrDefault(p => p.Key == "width").Value;
        var paramHeight = session.Parameters.FirstOrDefault(p => p.Key == "height").Value;
            
        logger.LogTrace("AppCommands.WindowResized(URL): width={ParamWidth}, height={ParamHeight}", paramWidth, paramHeight);
        logger.LogTrace("AppCommands.WindowResized(binding): width={Width}, height={Height}", width, height);

        var chartVm = session.ViewModel as Charts1Vm;
        chartVm?.ChangeShowStacked();
    }
        
}