using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

// ReSharper disable ClassNeverInstantiated.Global

namespace IctBaden.Stonehenge.Vue.SampleCore;

public class StonehengeRawContent
{
    private readonly RequestDelegate _next;

    // ReSharper disable once UnusedMember.Global
    public StonehengeRawContent(RequestDelegate next)
    {
        _next = next;
    }

    // ReSharper disable once UnusedMember.Global
    public async Task Invoke(HttpContext context)
    {
        var logger = context.Items["stonehenge.Logger"] as ILogger;
        var options = context.Items["stonehenge.HostOptions"] as StonehengeHostOptions;

        var path = context.Request.Path.Value;

        if (path.StartsWith("/metrics"))
        {
            var response = context.Response.Body;

            context.Response.Headers.Add("Cache-Control", new[] { "no-cache" });
            context.Response.Headers.Add("Content-Type", MediaTypeNames.Text.Plain);

            await using var writer = new StreamWriter(response);
            await writer.WriteAsync("test test test test test test");

            return;
        }

        await _next.Invoke(context);
    }
}