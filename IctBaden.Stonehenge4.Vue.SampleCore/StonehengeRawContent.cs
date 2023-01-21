using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

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