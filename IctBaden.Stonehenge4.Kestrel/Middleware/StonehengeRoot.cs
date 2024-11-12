using System.Threading.Tasks;
using System.Web;
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
            var query = HttpUtility.ParseQueryString(context.Request.QueryString.ToString());
            var uri = "/index.html";
            if(query.Count > 0) uri += $"?{query}"; 
            context.Response.Redirect(uri);
            return;
        }

        await _next.Invoke(context).ConfigureAwait(false);
    }
}