using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Kestrel.Middleware;
using IctBaden.Stonehenge.Resources;
using IctBaden.Stonehenge.ViewModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem
// ReSharper disable StringLiteralTypo

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

namespace IctBaden.Stonehenge.Kestrel;

[SuppressMessage("Design", "MA0051:Method is too long")]
public sealed class KestrelHost : IStonehengeHost, IDisposable
{
    public string BaseUrl { get; private set; } = string.Empty;

    private readonly AppSessions _appSessions = new();

    public AppSession[] GetAllSessions() => _appSessions.GetAllSessions();

    private WebApplication? _webApp;
    private Task? _host;
    private CancellationTokenSource? _cancel;

    private readonly IStonehengeResourceProvider _resourceProvider;
    private readonly StonehengeHostOptions _options;
    private readonly ILogger _logger;

    public KestrelHost(IStonehengeResourceProvider provider)
        : this(provider, new StonehengeHostOptions())
    {
    }

    public KestrelHost(IStonehengeResourceProvider provider, StonehengeHostOptions options)
        : this(provider, options, StonehengeLogger.DefaultLogger)
    {
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public KestrelHost(IStonehengeResourceProvider provider, StonehengeHostOptions options, ILogger logger)
    {
        _resourceProvider = provider;
        _options = options;
        _logger = logger;

        var isAspNetCoreApp = true;
        var ctx = AppContext.GetData("APP_CONTEXT_DEPS_FILES")?.ToString() ?? string.Empty;
        if (!string.IsNullOrEmpty(ctx) && !ctx.Contains("Microsoft.AspNetCore.App"))
        {
            isAspNetCoreApp = false;
            if (File.Exists(ctx))
            {
                var dependencies = File.ReadAllText(ctx);
                isAspNetCoreApp = dependencies.Contains("Microsoft.AspNetCore.App");
            }
        }

        if (!isAspNetCoreApp)
        {
            throw new NotSupportedException(
                "Project has to be based on web SDK: <Project Sdk=\"Microsoft.NET.Sdk.Web\">" +
                Environment.NewLine + "AppContext=" + ctx);
        }

        if (provider is StonehengeResourceLoader loader)
        {
            provider.InitProvider(loader, options);
        }
    }

    public void Dispose()
    {
        (_webApp as IHost)?.Dispose();
        _host?.Dispose();
        _cancel?.Dispose();
    }

    public bool Start(string hostAddress, int hostPort = 0)
    {
        try
        {
            _logger.LogInformation("KestrelHost.Start({HostAddress}, {HostPort})", hostAddress, hostPort);
            if (hostPort == 0)
            {
                hostPort = Network.GetFreeTcpPort();
            }

            IPAddress kestrelAddress;
            var useSsl = File.Exists(_options.SslCertificatePath);
            if (!string.IsNullOrEmpty(_options.SslCertificatePath))
            {
                if (useSsl)
                {
                    _logger.LogInformation("KestrelHost.Start: Using SSL using certificate {SslCertificatePath}",
                        _options.SslCertificatePath);
                }
                else
                {
                    _logger.LogError("KestrelHost.Start: NOT using SSL - certificate not found: {SslCertificatePath}",
                        _options.SslCertificatePath);
                }
            }

            var protocol = useSsl ? "https" : "http";
            string httpSysAddress;
            switch (hostAddress)
            {
                case null:
                case "*":
                    kestrelAddress = IPAddress.Any;
                    httpSysAddress = $"{protocol}://+:{hostPort}";
                    BaseUrl = $"{protocol}://{IPAddress.Loopback}:{hostPort}";
                    break;
                case "localhost":
                    kestrelAddress = IPAddress.Loopback;
                    httpSysAddress = $"{protocol}://{kestrelAddress}:{hostPort}";
                    BaseUrl = $"{protocol}://{kestrelAddress}:{hostPort}";
                    break;
                default:
                    kestrelAddress = IPAddress.Parse(hostAddress);
                    httpSysAddress = $"{protocol}://{kestrelAddress}:{hostPort}";
                    BaseUrl = $"{protocol}://{kestrelAddress}:{hostPort}";
                    break;
            }

            var mem = new MemoryConfigurationSource
            {
                InitialData =
                [
                    new KeyValuePair<string, string?>("AppTitle", _options.Title),
                    new KeyValuePair<string, string?>("HostOptions", JsonSerializer.Serialize(_options))
                ]
            };

            var config = new ConfigurationBuilder()
                .Add(mem)
                .Build();

            var builder = WebApplication.CreateBuilder();
            var services = builder.Services;
            services.AddSingleton(_logger);
            services.AddSingleton<IConfiguration>(config);
            services.AddSingleton(_appSessions);
            services.AddSingleton(_resourceProvider);

            services.AddResponseCompression(options =>
            {
                options.Providers.Add<GzipCompressionProvider>();
                options.EnableForHttps = true;
            });
            services.AddCors(o => o.AddPolicy("StonehengePolicy", policy => policy
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowAnyOrigin()));

            builder.WebHost.ConfigureKestrel(options =>
            {
                _logger.LogInformation("KestrelHost.Start: Using Kestrel");
                options.UseSystemd();
                // ensure no connection limit
                options.Limits.MaxConcurrentConnections = null;
                options.Listen(kestrelAddress, hostPort, listenOptions =>
                {
                    if (useSsl)
                    {
                        listenOptions.UseHttps(
                            _options.SslCertificatePath,
                            _options.SslCertificatePassword);
                    }
                });
            });

            if (Environment.OSVersion.Platform == PlatformID.Win32NT && _options.UseNtlmAuthentication)
            {
                _logger.LogInformation("KestrelHost.Start: Using NTLM authentication");
                WindowsHosting.UseNtlmAuthentication(builder.WebHost, httpSysAddress);
            }

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                _logger.LogInformation("KestrelHost.Start: Enable hosting in IIS");
                WindowsHosting.EnableIIS(builder.WebHost);
            }

            (_webApp as IHost)?.Dispose();
            _webApp = builder.Build();

            ConfigureWebApplication(_webApp, config);

            _cancel?.Dispose();
            _cancel = new CancellationTokenSource();
            _host = _webApp.RunAsync(_cancel.Token);

            if (_host.IsFaulted)
            {
                if (_host.Exception != null)
                {
                    throw _host.Exception;
                }
            }

            _logger.LogInformation("KestrelHost.Start: Listening on {Protocol}://{Address}:{Port}", 
                protocol,
                kestrelAddress.ToString().Replace("0.0.0.0", "127.0.0.1", StringComparison.OrdinalIgnoreCase),
                hostPort);
            _logger.LogInformation("KestrelHost.Start: succeeded");
        }
        catch (Exception ex)
        {
            if (ex.InnerException is HttpListenerException { ErrorCode: 5 })
            {
                _logger.LogError("Access denied: Try netsh http delete urlacl {BaseUrl}", BaseUrl);
            }
            else if (ex is MissingMemberException && ex.Message.Contains("Microsoft.Owin.Host.HttpListener"))
            {
                _logger.LogError("Missing reference to nuget package 'Microsoft.Owin.Host.HttpListener'");
            }

            var message = ex.Message;
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
                message += Environment.NewLine + "    " + ex.Message;
            }

            _logger.LogError("KestrelHost.Start: {Message}", message);
            _host?.Dispose();
            _host = null;

            Debugger.Break();
        }

        return _host != null;
    }

    private void ConfigureWebApplication(WebApplication webApp, IConfigurationRoot config)
    {
        webApp.UseMiddleware<ServerExceptionLogger>();
        webApp.UseMiddleware<StonehengeAcme>();
        webApp.Use((context, next) =>
        {
            context.Items.Add("stonehenge.Logger", _logger);
            context.Items.Add("stonehenge.AppSessions", _appSessions);
            context.Items.Add("stonehenge.AppTitle", config["AppTitle"] ?? string.Empty);
            context.Items.Add("stonehenge.HostOptions", _options);
            context.Items.Add("stonehenge.ResourceLoader", _resourceProvider);
            return next.Invoke();
        });
        foreach (var cm in _options.CustomMiddleware)
        {
            var cmType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(type => string.Equals(type.Name, cm, StringComparison.Ordinal));
            if (cmType != null)
            {
                webApp.UseMiddleware(cmType);
            }
        }

        webApp.UseResponseCompression();
        webApp.UseCors("StonehengePolicy");
        webApp.UseMiddleware<StonehengeSession>();
        webApp.UseMiddleware<StonehengeHeaders>();
        webApp.UseMiddleware<StonehengeRoot>();

        webApp.UseMiddleware<ServerSentEvents>();
        webApp.UseMiddleware<StonehengeContent>();
    }

    public void Terminate()
    {
        _logger.LogInformation("KestrelHost.Terminate: Cancel WebApp");
        _cancel?.Cancel();

        try
        {
            _logger.LogInformation("KestrelHost.Terminate: Host...");
            _host?.Wait();
            _host?.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Debugger.Break();
        }

        try
        {
            _logger.LogInformation("KestrelHost.Terminate: Web_webApp...");
            (_webApp as IHost)?.Dispose();
            _webApp = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Debugger.Break();
        }

        _logger.LogInformation("KestrelHost.Terminate: Terminated");
    }

    public void SetLogLevel(LogLevel level)
    {
        // TODO
    }

    public void EnableRoute(string route, bool enabled)
    {
        var sessions = _appSessions.GetAllSessions();
        foreach (var viewModel in sessions.Select(session => session.ViewModel as ActiveViewModel))
        {
            viewModel?.EnableRoute(route, enabled);
        }
    }
}