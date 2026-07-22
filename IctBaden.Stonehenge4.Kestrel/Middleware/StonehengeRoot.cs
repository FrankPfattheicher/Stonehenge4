using System.Threading.Tasks;
using System.Web;
using IctBaden.Stonehenge.Hosting;
using Microsoft.AspNetCore.Http;

namespace IctBaden.Stonehenge.Kestrel.Middleware;

// ReSharper disable once ClassNeverInstantiated.Global
public class StonehengeRoot
{
    private readonly RequestDelegate _next;

    // ReSharper disable once UnusedMember.Global
    public StonehengeRoot(RequestDelegate next)
    {
        _next = next;
    }

    // ReSharper disable once UnusedMember.Global
    public async Task Invoke(HttpContext context)
    {
        var path = context.Request.Path.Value?.Replace("//", "/", System.StringComparison.OrdinalIgnoreCase);
        if (string.Equals(path, "/", System.StringComparison.Ordinal))
        {
            var options = context.Items["stonehenge.HostOptions"] as StonehengeHostOptions ?? new StonehengeHostOptions();
            var query = HttpUtility.ParseQueryString(context.Request.QueryString.ToString());
            var uri = options.BasePath + "/index.html";
            if(query.Count > 0) uri += $"?{query}"; 
            context.Response.Redirect(uri);
            return;
        }
        if (path != null && path.EndsWith(".map", System.StringComparison.OrdinalIgnoreCase))
        {
            context.Response.ContentType = "application/json";
            await context.Response
                .WriteAsync("{}", cancellationToken: context.RequestAborted)
                .ConfigureAwait(StonehengeGlobal.ConfigureAwait);
            return;
        }

        await _next.Invoke(context).ConfigureAwait(StonehengeGlobal.ConfigureAwait);
    }
}