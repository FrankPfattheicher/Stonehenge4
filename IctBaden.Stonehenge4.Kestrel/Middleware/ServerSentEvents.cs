using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace IctBaden.Stonehenge.Kestrel.Middleware;

// ReSharper disable once ClassNeverInstantiated.Global
public class ServerSentEvents
{
    private readonly RequestDelegate _next;

    public ServerSentEvents(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (string.Equals(context.Request.Method, "GET", StringComparison.OrdinalIgnoreCase) && 
            path.StartsWith("/EventSource/", StringComparison.OrdinalIgnoreCase))
        {
            var logger = context.Items["stonehenge.Logger"] as ILogger;
            logger?.LogDebug("EventSource Request: {Path}", path);

            var appSession = context.Items["stonehenge.AppSession"] as AppSession;
            if (appSession == null)
            {
                logger?.LogDebug("EventSource Request - NO SESSION");
                await _next.Invoke(context).ConfigureAwait(false);
                return;
            }
            var viewModel = appSession.ViewModel as ActiveViewModel;
            if (viewModel == null)
            {
                logger?.LogDebug("EventSource Request({VmName}) - VM is no ActiveViewModel", appSession.ViewModel?.GetType().Name ?? "<NONE>");
                await _next.Invoke(context).ConfigureAwait(false);
                return;
            }
            
            var rqVmName = path.Substring(13);
            var appVmName = appSession.ViewModel?.GetType().Name ?? string.Empty;
            if (!string.Equals(rqVmName, appVmName, StringComparison.OrdinalIgnoreCase))
            {
                logger?.LogDebug("EventSource Request({RqVmName}) - FAIL: Active VM is {AppVmName}", rqVmName, appVmName);
                // await viewModel.CancelPropertiesChanged().ConfigureAwait(false);
                await Terminate(context).ConfigureAwait(false);
                return;
            }

            if (viewModel.SendingPropertiesChanged())
            {
                logger?.LogDebug("EventSource Request({RqVmName}) - New request - Terminate previous", rqVmName);
                await viewModel.CancelPropertiesChanged().ConfigureAwait(false);
            }
            context.Response.Headers.Append("Content-Type", "text/event-stream");
            await viewModel.SendPropertiesChanged(context).ConfigureAwait(false);

            if (viewModel.SendingPropertiesChanged())
            {
                logger?.LogDebug("EventSource Request({RqVmName}) - Terminating", rqVmName);
                await viewModel.CancelPropertiesChanged().ConfigureAwait(false);
            }
            await Terminate(context).ConfigureAwait(false);
            return;
        }

        await _next.Invoke(context).ConfigureAwait(false);
    }

    private static async Task Terminate(HttpContext context)
    {
        const string json = "data: { \"StonehengeContinuePolling\": false }\r\r";
        await context.Response.WriteAsync(json).ConfigureAwait(false);
        await context.Response.Body.FlushAsync().ConfigureAwait(false);
        context.Response.Body.Close();
        await context.Response.CompleteAsync().ConfigureAwait(false);
        context.Abort();
    }
    
}