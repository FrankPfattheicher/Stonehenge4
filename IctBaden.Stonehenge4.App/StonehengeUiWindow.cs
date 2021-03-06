using System;
using System.Threading;
using IctBaden.Stonehenge.Hosting;
using Microsoft.Extensions.Logging;

namespace IctBaden.Stonehenge.App;

public class StonehengeUiWindow : IDisposable
{
    private readonly ILogger _logger;
    private readonly StonehengeHostOptions _options;
    private readonly StonehengeUi _ui;
    private HostWindow _wnd;

    public StonehengeUiWindow(ILogger logger, StonehengeHostOptions options)
    {
        _logger = logger;
        _options = options;
        _ui = new StonehengeUi(logger, options);
    }
    
    /// <summary>
    /// Start window process with
    /// random free port, not public reachable
    /// and default size 800 x 600
    /// </summary>
    /// <returns>false if failed to start window process</returns>
    public bool Start() => Start(0, false, new Point(800, 600));

    /// <summary>
    /// Start window process
    /// does not return until window is closed or Ctrl+C pressed 
    /// </summary>
    /// <param name="port">port to listen on</param>
    /// <param name="publicReachable">enables public reachable hosting</param>
    /// <param name="windowSize">definition of initial window size</param>
    /// <returns>false if failed to start window process</returns>
    public bool Start(int port, bool publicReachable, Point windowSize)
    {
        if(!_ui.Start(port, publicReachable)) return false;

        _wnd = new HostWindow(_ui.Server.BaseUrl, _options.Title, windowSize);
        if(!_wnd.Open()) return false;
        
        var terminate = new AutoResetEvent(false);
        Console.CancelKeyPress += (_, _) => { terminate.Set(); };
        terminate.WaitOne();
        return true;
    }
    
    public void Dispose()
    {
        _wnd?.Dispose();
        _ui?.Dispose();
    }
}