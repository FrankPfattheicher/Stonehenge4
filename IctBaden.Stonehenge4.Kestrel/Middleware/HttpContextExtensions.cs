using System.Net;
using Microsoft.AspNetCore.Http;

namespace IctBaden.Stonehenge.Kestrel.Middleware;

internal static class HttpContextExtensions
{
    public static bool IsLocal(this HttpContext ctx)
    {
        var connection = ctx.Connection;
        if (connection.RemoteIpAddress != null)
        {
            return connection.LocalIpAddress != null 
                ? connection.RemoteIpAddress.Equals(connection.LocalIpAddress) 
                : IPAddress.IsLoopback(connection.RemoteIpAddress);
        }

        // for in memory TestServer or when dealing with default connection info
        return connection.RemoteIpAddress == null && 
               connection.LocalIpAddress == null;
    }
}