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
        var path = context.Request.Path.Value ?? string.Empty;

        if (path.StartsWith("/metrics", System.StringComparison.InvariantCultureIgnoreCase))
        {
            var response = context.Response.Body;

            context.Response.Headers.CacheControl = new[] { "no-cache" };
            context.Response.Headers.ContentType = MediaTypeNames.Text.Plain;

            var writer = new StreamWriter(response);
            await using (writer.ConfigureAwait(false))
            {
                await writer.WriteAsync("test test test test test test").ConfigureAwait(false);

            return;
            }
        }

        await _next.Invoke(context).ConfigureAwait(false);
    }
}