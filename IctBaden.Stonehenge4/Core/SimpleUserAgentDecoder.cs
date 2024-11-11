using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace IctBaden.Stonehenge.Core;

[SuppressMessage("Security", "MA0009:Add regex evaluation timeout")]
internal partial class SimpleUserAgentDecoder
{
    public string BrowserName = string.Empty;
    public string BrowserVersion = string.Empty;

    public string ClientOsName = string.Empty;
    public string ClientOsVersion = string.Empty;

    public SimpleUserAgentDecoder(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return;

        try
        {
            DetectBrowser(userAgent);
            DetectOperatingSystem(userAgent);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            Debugger.Break();
        }
    }

    private void DetectBrowser(string userAgent)
    {
        var edge = RegexEdge()
            .Match(userAgent);
        if (edge.Success)
        {
            BrowserName = "Edge";
            BrowserVersion = edge.Groups[1].Value;
            return;
        }

        var chrome = RegexChrome()
            .Match(userAgent);
        if (chrome.Success)
        {
            BrowserName = "Chrome";
            BrowserVersion = chrome.Groups[1].Value;
            return;
        }

        var firefox = RegexFirefox()
            .Match(userAgent);
        if (firefox.Success)
        {
            BrowserName = "Firefox";
            BrowserVersion = firefox.Groups[1].Value;
            return;
        }
        
        BrowserName = userAgent;
    }
 
    private void DetectOperatingSystem(string userAgent)
    {
        var windows = RegexWindows()
            .Match(userAgent);
        if (windows.Success)
        {
            ClientOsName = "Windows";
            ClientOsVersion = windows.Groups[1].Value;
            return;
        }

        var linux = RegexLinux()
            .Match(userAgent);
        if (linux.Success)
        {
            ClientOsName = linux.Groups[1].Value;
            if (string.Equals(ClientOsName, "X11", StringComparison.OrdinalIgnoreCase))
            {
                ClientOsName = "Debian";
            }
            else if (string.IsNullOrEmpty(ClientOsName))
            {
                ClientOsName = "Linux";
            }
            return;
        }

        ClientOsName = userAgent;
    }

    [GeneratedRegex(@"Edg/([0-9\.]+)")]
    private static partial Regex RegexEdge();
    [GeneratedRegex(@"Chrome/([0-9\\.]+)")]
    private static partial Regex RegexChrome();
    [GeneratedRegex(@"Firefox/([0-9\.]+)")]
    private static partial Regex RegexFirefox();
    [GeneratedRegex(@"Windows NT ([0-9\.]+)")]
    private static partial Regex RegexWindows();
    [GeneratedRegex(@"(([a-zA-Z]+)|X11); Linux")]
    private static partial Regex RegexLinux();
}