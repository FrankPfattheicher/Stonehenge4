using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;

// ReSharper disable NotAccessedField.Global
// ReSharper disable UnusedMember.Global

// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo

namespace IctBaden.Stonehenge.Hosting;

public sealed class HostWindow : IDisposable
{
    private readonly string _title;
    private readonly Point _windowSize;
    private readonly ILogger _logger;
    private readonly string _startUrl;
    private Process? _ui;

    public string Browser = string.Empty;

    // ReSharper disable once MemberCanBePrivate.Global
    public string LastError = string.Empty;
    public int ProcessId => _ui?.Id ?? 0;
    public IntPtr MainWindowHandle => _ui?.MainWindowHandle ?? IntPtr.Zero;

    private static Point DefaultWindowSize
    {
        get
        {
            //var screen = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            //if (screen.Width > 1024)
            //{
            //    screen.Width = 1024;
            //    screen.Height = 768;
            //}
            //else if (screen.Width > 800)
            //{
            //    screen.Width = 800;
            //    screen.Height = 600;
            //}
            //return new Point(screen.Width, screen.Height);

            // ReSharper disable once ArrangeAccessorOwnerBody
            return new Point(800, 600);
        }
    }

    public HostWindow()
        : this(StonehengeLogger.DefaultLogger, "http://localhost", "", DefaultWindowSize)
    {
    }

    public HostWindow(ILogger logger)
        : this(logger, "http://localhost", "", DefaultWindowSize)
    {
    }

    public HostWindow(ILogger logger, string startUrl)
        : this(logger, startUrl, "", DefaultWindowSize)
    {
    }

    public HostWindow(ILogger logger, string startUrl, string title)
        : this(logger, startUrl, title, DefaultWindowSize)
    {
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public HostWindow(ILogger logger, string startUrl, string title, Point windowSize)
    {
        _logger = logger;
        _startUrl = startUrl;
        _title = title;
        _windowSize = windowSize;

        AppDomain.CurrentDomain.ProcessExit += (_, _) => { Dispose(); };
    }

    public void Dispose()
    {
        try
        {
            _ui?.Kill(true);
            _ui?.Dispose();
        }
        catch
        {
            // ignore
        }
    }

    private void LogStart(string name)
    {
        Browser = name;
        _logger.LogInformation("AppHost [{Name}] created at {DateTime}, listening on {StartUrl}",
            name, DateTime.Now, _startUrl.Replace("0.0.0.0", "127.0.0.1"));
    }

    /// <summary>
    /// Open a UI window using an installed browser 
    /// in kino mode - if possible.
    /// This method does not return until the window is closed.
    /// </summary>
    /// <returns></returns>
    public bool Open()
    {
        _logger.LogInformation("Open main window on platform {Platform}", Environment.OSVersion.Platform);

        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var dir = Directory.CreateDirectory(path);

        var opened = false;
        if (!opened && !Environment.Is64BitOperatingSystem) opened = ShowWindowMidori();
        if (!opened) opened = ShowWindowEpiphany();
        if (!opened) opened = ShowWindowGoogleChrome(path);
        if (!opened) opened = ShowWindowChromium(path);
        if (!opened) opened = ShowWindowBrave(path);
        if (!opened) opened = ShowWindowEdge(path);
        if (!opened) opened = ShowWindowSafari();
        if (!opened) opened = ShowWindowFirefox();
        if (!opened) opened = ShowWindowInternetExplorer();

        if (!opened)
        {
            _logger.LogError("Could not create main window on platform {Platform}", Environment.OSVersion);
        }

        try
        {
            dir.Delete(recursive: true);
        }
        catch
        {
            // ignore
        }

        return opened;
    }

    private bool ShowWindowGoogleChrome(string path)
    {
        try
        {
            _logger.LogInformation("Trying Google Chrome");

            var pi = new ProcessStartInfo
            {
                FileName = Environment.OSVersion.Platform == PlatformID.Unix ? "google-chrome" : "chrome",
                CreateNoWindow = true,
                Arguments = "--disable-translate --new-window --no-default-browser-check --no-first-run "
                            + $"--app={_startUrl}/?title={HttpUtility.UrlEncode(_title)} --window-size={_windowSize.X},{_windowSize.Y} --user-data-dir=\"{path}\"",
                UseShellExecute = Environment.OSVersion.Platform != PlatformID.Unix
            };
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                pi.Arguments += " --disable-gpu";
            }

            _ui?.Dispose();
            _ui = Process.Start(pi);
            Thread.Sleep(100); // unexpected exit on linux
            if (_ui == null || _ui.HasExited)
            {
                return false;
            }

            LogStart(pi.FileName);
            _ui.WaitForExit();
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            _logger.LogError(ex, "Failed {Message}", ex.Message);
            return false;
        }
    }

    private bool ShowWindowChromium(string path)
    {
        try
        {
            _logger.LogInformation("Trying Chromium");

            var cmd = Environment.OSVersion.Platform == PlatformID.Unix ? "chromium-browser" : "chrome.exe";
            var parameter = "--disable-translate --new-window --no-default-browser-check --no-first-run "
                            + $"--app={_startUrl}/?title={HttpUtility.UrlEncode(_title)} --window-size={_windowSize.X},{_windowSize.Y} --user-data-dir=\"{path}\"";
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                parameter += " --disable-gpu";
            }

            _ui?.Dispose();
            _ui = Process.Start(cmd, parameter);
            if (_ui == null || _ui.HasExited)
            {
                return false;
            }

            LogStart(cmd);
            _ui.WaitForExit();
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            _logger.LogError(ex, "Failed {Message}", ex.Message);
            return false;
        }
    }


    private bool ShowWindowEdge(string path)
    {
        try
        {
            _logger.LogInformation("Trying Microsoft Edge");

            var pi = new ProcessStartInfo
            {
                FileName = "msedge",
                CreateNoWindow = true,
                Arguments =
                    $"--app={_startUrl}/?title={HttpUtility.UrlEncode(_title)} --window-size={_windowSize.X},{_windowSize.Y} --disable-translate --user-data-dir=\"{path}\"",
                UseShellExecute = Environment.OSVersion.Platform != PlatformID.Unix
            };
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                pi.Arguments += " --disable-gpu";
            }

            _ui?.Dispose();
            _ui = Process.Start(pi);
            Thread.Sleep(100); // unexpected exit on linux
            if ((_ui == null) || _ui.HasExited)
            {
                return false;
            }

            LogStart(pi.FileName);
            _ui.WaitForExit();
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            _logger.LogError(ex, "Failed {Message}", ex.Message);
            return false;
        }
    }


    private bool ShowWindowEpiphany()
    {
        if (Environment.OSVersion.Platform != PlatformID.Unix)
            return false;

        try
        {
            _logger.LogInformation("Trying Epiphany");

            var cmd = "epiphany";
            var parameter = $"{_startUrl}/?title={HttpUtility.UrlEncode(_title)}";
            _ui?.Dispose();
            _ui = Process.Start(cmd, parameter);
            if (_ui == null || _ui.HasExited)
            {
                return false;
            }

            LogStart(cmd);
            _ui.WaitForExit();
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            _logger.LogError(ex, "Failed {Message}", ex.Message);
            return false;
        }
    }

    private bool ShowWindowMidori()
    {
        if (Environment.OSVersion.Platform != PlatformID.Unix)
            return false;

        try
        {
            _logger.LogInformation("Trying Midori");

            var cmd = "midori";
            var parameter = $"-e Navigationbar -a {_startUrl}/?title={HttpUtility.UrlEncode(_title)}";
            _ui?.Dispose();
            _ui = Process.Start(cmd, parameter);
            Task.Delay(5000).Wait();
            if (_ui == null || _ui.HasExited)
            {
                return false;
            }

            LogStart(cmd);
            _ui.WaitForExit();
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            _logger.LogError(ex, "Failed {Message}", ex.Message);
            return false;
        }
    }

    private bool ShowWindowInternetExplorer()
    {
        if (Environment.OSVersion.Platform == PlatformID.Unix)
            return false;

        try
        {
            _logger.LogInformation("Trying Internet Explorer");

            const string cmd = "iexplore.exe";
            var parameter = $"-private {_startUrl}/?title={HttpUtility.UrlEncode(_title)}";
            _ui?.Dispose();
            _ui = Process.Start(cmd, parameter);
            if (_ui == null || _ui.HasExited)
            {
                return false;
            }

            LogStart(cmd);
            _ui.WaitForExit();
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            _logger.LogError(ex, "Failed {Message}", ex.Message);
            return false;
        }
    }

    private bool ShowWindowFirefox()
    {
        try
        {
            _logger.LogInformation("Trying Firefox");

            var cmd = Environment.OSVersion.Platform == PlatformID.Unix ? "firefox" : "firefox.exe";
            //var parameter = $"-new-instance --createprofile -url {_startUrl} --profile {path} -width {_windowSize.X} -height {_windowSize.Y}";
            var parameter = $"-no-remote -new-window {_startUrl} -width {_windowSize.X} -height {_windowSize.Y}";
            _ui?.Dispose();
            _ui = Process.Start(cmd, parameter);
            if (_ui == null || _ui.HasExited)
            {
                return false;
            }
            LogStart(cmd);
            _ui.WaitForExit();
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            _logger.LogError(ex, "Failed {Message}", ex.Message);
            return false;
        }
    }

    private bool ShowWindowSafari()
    {
        if (Environment.OSVersion.Platform != PlatformID.MacOSX && Environment.OSVersion.Platform != PlatformID.Other)
            return false;

        try
        {
            _logger.LogInformation("Trying Safari");

            const string cmd = "open";
            var parameter = $"-a Safari {_startUrl}";
            _ui?.Dispose();
            _ui = Process.Start(cmd, parameter);
            if (_ui == null || _ui.HasExited)
            {
                return false;
            }

            LogStart(cmd);
            _ui.WaitForExit();
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            _logger.LogError(ex, "Failed {Message}", ex.Message);
            return false;
        }
    }

    private bool ShowWindowBrave(string path)
    {
        try
        {
            _logger.LogInformation("Trying Brave");

            var pi = new ProcessStartInfo
            {
                FileName = Environment.OSVersion.Platform == PlatformID.Unix
                    ? "brave-browser"
                    : @"%programfiles%\BraveSoftware\Brave-Browser\Application\brave.exe",
                CreateNoWindow = true,
                Arguments =
                    "--disable-translate --new-window --no-default-browser-check --no-first-run --disable_brave_extension --disable-features=P3A "
                    + $"--app={_startUrl}/?title={HttpUtility.UrlEncode(_title)} --window-size={_windowSize.X},{_windowSize.Y} --user-data-dir=\"{path}\"",
                UseShellExecute = Environment.OSVersion.Platform != PlatformID.Unix
            };
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                pi.Arguments += " --disable-gpu";
            }

            _ui?.Dispose();
            _ui = Process.Start(pi);
            Thread.Sleep(100); // unexpected exit on linux
            if (_ui == null || _ui.HasExited)
            {
                return false;
            }

            LogStart(pi.FileName);
            _ui.WaitForExit();
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            _logger.LogError(ex, "Failed {Message}", ex.Message);
            return false;
        }
    }
}