using System.IO;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
// ReSharper disable ClassNeverInstantiated.Global

namespace IctBaden.Stonehenge.Kestrel.Middleware;

public class StonehengeAcme
{
    private readonly RequestDelegate _next;

    // ReSharper disable once UnusedMember.Global
    public StonehengeAcme(RequestDelegate next)
    {
        _next = next;
    }

    // ReSharper disable once UnusedMember.Global
    public async Task Invoke(HttpContext context)
    {
        var logger = context.Items["stonehenge.Logger"] as ILogger;
            
        var path = context.Request.Path.Value;
        if (path != null && path.StartsWith("/.well-known", System.StringComparison.OrdinalIgnoreCase))
        {
            var response = context.Response.Body;

            var rootPath = StonehengeApplication.BaseDirectory;
            var acmeFile = rootPath + context.Request.Path.Value;
            if (File.Exists(acmeFile))
            {
                context.Response.Headers.Append("Cache-Control", (string[]) ["no-cache"]);

                var acmeData = await File.ReadAllBytesAsync(acmeFile).ConfigureAwait(false);
                var writer = new BinaryWriter(response);
                await using (writer.ConfigureAwait(false))
                {
                    writer.Write(acmeData);

                return;
                }
            }

            logger?.LogError("No ACME data found");
        }

        await _next.Invoke(context).ConfigureAwait(false);
    }

}