using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace IctBaden.Stonehenge.Caching.Client;

// ReSharper disable once UnusedType.Global
[SuppressMessage("Security", "MA0009:Add regex evaluation timeout")]
[SuppressMessage("Performance", "MA0098:Use indexer instead of LINQ methods")]
public static partial class ContentDeliveryNetworkSupport
{
    private const string CdnConfigurationFileName = "CDN.cfg";
    private static Dictionary<string, string>? _cdnLookup;

    public static IDictionary<string, string> CdnLookup
    {
        get
        {
            if (_cdnLookup != null)
                return _cdnLookup;

            if (File.Exists(CdnConfigurationFileName))
            {
                _cdnLookup = (from line in File.ReadAllLines(CdnConfigurationFileName)
                    where !line.StartsWith('#')
                    let elements = line.Split('=', StringSplitOptions.RemoveEmptyEntries)
                    where elements.Length == 2
                    select elements).ToDictionary(e => e[0], e => e[1], StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                _cdnLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return _cdnLookup;
        }
        set => _cdnLookup = (Dictionary<string, string>?)value;
    }

    public static string ResolveHostsHtml(string page, bool isSecureConnection)
    {
        if (!File.Exists(CdnConfigurationFileName))
            return page;

        var protocol = isSecureConnection ? "https://" : "http://";
        var script = RegexScript1();

        // ReSharper disable once CanSimplifyDictionaryLookupWithTryGetValue
        var resultLines = from line in page.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            let isScriptSource = script.Match(line)
            let source = isScriptSource.Groups["c"].Value.Split('/')[^1]
            select isScriptSource.Success && CdnLookup.ContainsKey(source) ?
                isScriptSource.Groups["a"].Value.Replace(isScriptSource.Groups["b"].Value, CdnLookup[source].Replace("http://", protocol, StringComparison.OrdinalIgnoreCase), StringComparison.OrdinalIgnoreCase) : line;

        return string.Join(Environment.NewLine, resultLines);
    }

    public static string ResolveHostsJs(string page, bool isSecureConnection)
    {
        if (!File.Exists(CdnConfigurationFileName))
            return page;

        var protocol = isSecureConnection ? "https://" : "http://";
        var script = RegexScript2();

        // ReSharper disable once CanSimplifyDictionaryLookupWithTryGetValue
        var resultLines = from line in page.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            let isMapPath = script.Match(line)
            let source = isMapPath.Groups["path"].Value.Split('/').Last() + ".js"
            select isMapPath.Success && CdnLookup.ContainsKey(source) ?
                isMapPath.Groups["map"].Value.Replace(isMapPath.Groups["path"].Value, CdnLookup[source].Replace("http://", protocol)).Replace(".js'", "'") : line;

        return string.Join(Environment.NewLine, resultLines);
    }

    [GeneratedRegex("(?<a><script.*src=\"(?<b>(?<c>.*\\.js))\".*)|(?<a><link.*href=\"(?<b>(?<c>.*\\.css))\".*)", RegexOptions.Compiled)]
    private static partial Regex RegexScript1();
    [GeneratedRegex("(?<map>'(?<id>.+)' *: *'(?<path>.*)'.*)", RegexOptions.Compiled)]
    private static partial Regex RegexScript2();
}