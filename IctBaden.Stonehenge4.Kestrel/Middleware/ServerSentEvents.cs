using System;
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
                await _next.Invoke(context).ConfigureAwait(Program.ConfigureAwait);
                return;
            }
            var viewModel = appSession.ViewModel as ActiveViewModel;
            if (viewModel == null)
            {
                logger?.LogDebug("EventSource Request({VmName}) - VM is no ActiveViewModel", appSession.ViewModel?.GetType().Name ?? "<NONE>");
                await _next.Invoke(context).ConfigureAwait(Program.ConfigureAwait);
                return;
            }
            
            var rqVmName = path.Substring(13);
            var appVmName = appSession.ViewModel?.GetType().Name ?? string.Empty;
            if (!string.Equals(rqVmName, appVmName, StringComparison.OrdinalIgnoreCase))
            {
                logger?.LogDebug("EventSource Request({RqVmName}) - FAIL: Active VM is {AppVmName}", rqVmName, appVmName);
                // await viewModel.CancelPropertiesChanged().ConfigureAwait(Program.ConfigureAwait);
                await Terminate(context).ConfigureAwait(Program.ConfigureAwait);
                return;
            }

            if (viewModel.SendingPropertiesChanged())
            {
                logger?.LogDebug("EventSource Request({RqVmName}) - New request - Terminate previous", rqVmName);
                await viewModel.CancelPropertiesChanged().ConfigureAwait(Program.ConfigureAwait);
            }
            context.Response.Headers.Append("Content-Type", "text/event-stream");
            await viewModel.SendPropertiesChanged(context).ConfigureAwait(Program.ConfigureAwait);

            if (viewModel.SendingPropertiesChanged())
            {
                logger?.LogDebug("EventSource Request({RqVmName}) - Terminating", rqVmName);
                await viewModel.CancelPropertiesChanged().ConfigureAwait(Program.ConfigureAwait);
            }
            await Terminate(context).ConfigureAwait(Program.ConfigureAwait);
            return;
        }

        await _next.Invoke(context).ConfigureAwait(Program.ConfigureAwait);
    }

    private static async Task Terminate(HttpContext context)
    {
        const string json = "data: { \"StonehengeContinuePolling\": false }\r\r";
        await context.Response.WriteAsync(json).ConfigureAwait(Program.ConfigureAwait);
        await context.Response.Body.FlushAsync().ConfigureAwait(Program.ConfigureAwait);
        context.Response.Body.Close();
        await context.Response.CompleteAsync().ConfigureAwait(Program.ConfigureAwait);
        context.Abort();
    }
    
}