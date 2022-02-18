using System.Diagnostics.CodeAnalysis;
using System.Linq;
using IctBaden.Stonehenge4.Core;
using IctBaden.Stonehenge4.Hosting;
using IctBaden.Stonehenge4.ViewModel;
using Microsoft.Extensions.Logging;
// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace IctBaden.Stonehenge4.Vue.SampleCore
{
    // ReSharper disable once UnusedMember.Global
    [SuppressMessage("Usage", "CA2254:Vorlage muss ein statischer Ausdruck sein")]
    public class AppCommands : IStonehengeAppCommands
    {
        private readonly ILogger _logger;

        public AppCommands(ILogger logger)
        {
            _logger = logger;
        }
        
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
            
            _logger.LogTrace($"AppCommands.WindowResized(URL): width={paramWidth}, height={paramHeight}");
            _logger.LogTrace($"AppCommands.WindowResized(binding): width={width}, height={height}");
        }
        
    }
}
