using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace IctBaden.Stonehenge.Kestrel.Middleware;

[SuppressMessage("Usage", "CA2254:Vorlage muss ein statischer Ausdruck sein")]
[SuppressMessage("ReSharper", "ReplaceSubstringWithRangeIndexer")]
public class StonehengeHeaders
{
    private readonly RequestDelegate _next;
    private static Dictionary<string, string>? _headers;

    // ReSharper disable once UnusedMember.Global
    public StonehengeHeaders(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var logger = context.Items["stonehenge.Logger"] as ILogger;
        if (logger == null)
        {
            Debugger.Break();
            return;
        }
            
        try
        {
            if (_headers == null)
            {
                LoadHeaders(logger);
            }
            if (_headers != null)
            {
                foreach (var header in _headers)
                {
                    context.Response.Headers.Append(header.Key, header.Value);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError("Error handling default headers: {Message}\r\n{StackTrace}", ex.Message, ex.StackTrace);
            Debugger.Break();
        }
        await _next.Invoke(context).ConfigureAwait(false);
    }

    private void LoadHeaders(ILogger logger)
    {
        _headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var path = StonehengeApplication.BaseDirectory;
        var headersFile = Path.Combine(path, "defaultheaders.txt");
        if (!File.Exists(headersFile)) return;

        logger.LogDebug("Adding default headers from: " + headersFile);
        var headers = File.ReadAllLines(headersFile);
        foreach (var header in headers)
        {
            if (string.IsNullOrEmpty(header)) continue;
            if (header.StartsWith('#')) continue;

            var colon = header.IndexOf(':', StringComparison.OrdinalIgnoreCase);
            if (colon < 1) continue;
            var key = header.Substring(0, colon).Trim();
            var value = header.Substring(colon + 1).Trim();
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
            {
                logger.LogDebug("Add header: {Key}: {Value}", key, value);
                _headers.Add(key, value);
            }
        }
    }
}