using System.Linq;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.ViewModel;
using Microsoft.Extensions.Logging;

namespace IctBaden.Stonehenge.Vue.SampleCore;

// ReSharper disable once UnusedType.Global
public class AppCommands : IStonehengeAppCommands
{
    private readonly ILogger _logger;

    public AppCommands(ILogger logger)
    {
        _logger = logger;
    }

    // ReSharper disable once UnusedMember.Global
    public void FileOpen(AppSession session, string param)
    {
        var vm = session.ViewModel as ActiveViewModel;
        vm?.MessageBox("AppCommand", $"FileOpen({param})");
    }
        
    public void WindowResized(AppSession session, int width, int height)
    {
        var paramWidth = session.Parameters
            .FirstOrDefault(p => string.Equals(p.Key, "width", System.StringComparison.OrdinalIgnoreCase)).Value;
        var paramHeight = session.Parameters
            .FirstOrDefault(p => string.Equals(p.Key, "height", System.StringComparison.OrdinalIgnoreCase)).Value;
            
        _logger.LogTrace("AppCommands.WindowResized(URL): width={ParamWidth}, height={ParamHeight}", paramWidth, paramHeight);
        _logger.LogTrace("AppCommands.WindowResized(binding): width={Width}, height={Height}", width, height);

        // Obsolete - use OnWindowResized
        // var chartVm = session.ViewModel as Charts1Vm;
        // chartVm?.ChangeShowStacked();
    }
        
}