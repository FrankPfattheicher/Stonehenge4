using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.Hosting;

namespace IctBaden.Stonehenge.Kestrel;

[SuppressMessage("Interoperability", "CA1416:Plattformkompatibilität überprüfen")]
public static class WindowsHosting
{
    /// <summary>
    /// Host ASP.NET Core on Windows with IIS
    /// https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/?view=aspnetcore-2.2#enable-the-iisintegration-components
    /// </summary>
    /// <param name="builder"></param>
    /// <returns>builder</returns>
    // ReSharper disable once InconsistentNaming
    public static IHostBuilder EnableIIS(IHostBuilder builder) => builder
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseIIS(); // in-proc hosting
            webBuilder.UseIISIntegration(); // out-of-proc hosting   
        });

    public static IHostBuilder UseNtlmAuthentication(IHostBuilder builder, string httpSysAddress) => builder
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseHttpSys(options =>
            {
                // netsh http add urlacl url=https://+:32000/ user=TheUser
                options.Authentication.Schemes =
                    (AuthenticationSchemes)(System.Net.AuthenticationSchemes.Ntlm |
                                            System.Net.AuthenticationSchemes.Negotiate);
                options.Authentication.AllowAnonymous = false;
                options.UrlPrefixes.Add(httpSysAddress);
            });
        });
}