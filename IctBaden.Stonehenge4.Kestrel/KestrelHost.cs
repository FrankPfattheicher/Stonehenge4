using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Resources;
using IctBaden.Stonehenge.ViewModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem
// ReSharper disable StringLiteralTypo

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

namespace IctBaden.Stonehenge.Kestrel;

public sealed class KestrelHost : IStonehengeHost, IDisposable
{
    public string BaseUrl { get; private set; } = string.Empty;
    
    private readonly AppSessions _appSessions = new();

    public AppSession[] GetAllSessions() => _appSessions.GetAllSessions();
    
    private IWebHost? _webApp;
    private Task? _host;
    private CancellationTokenSource? _cancel;

    private readonly IStonehengeResourceProvider _resourceProvider;
    private readonly StonehengeHostOptions _options;
    private readonly ILogger _logger;
    private Startup? _startup;

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
            throw new NotSupportedException("Project has to be based on web SDK: <Project Sdk=\"Microsoft.NET.Sdk.Web\">" + 
                                            Environment.NewLine + "AppContext=" + ctx);
        }

        if (provider is StonehengeResourceLoader loader)
        {
            provider.InitProvider(loader, options);
        }
    }

    public void Dispose()
    {
        _webApp?.Dispose();
        _host?.Dispose();
        _cancel?.Dispose();
    }

    public bool Start(string hostAddress, int hostPort)
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
            if(!string.IsNullOrEmpty(_options.SslCertificatePath))
            {
                if (useSsl)
                {
                    _logger.LogInformation("KestrelHost.Start: Using SSL using certificate {SslCertificatePath}", _options.SslCertificatePath);
                }
                else
                {
                    _logger.LogError("KestrelHost.Start: NOT using SSL - certificate not found: {SslCertificatePath}", _options.SslCertificatePath);
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

            _startup = new Startup(_logger, config, _resourceProvider, _appSessions);
                
            var builder = new WebHostBuilder()
                .UseConfiguration(config)
                .ConfigureServices(s => { s.AddSingleton(_logger); })
                .ConfigureServices(s => { s.AddSingleton<IConfiguration>(config); })
                .ConfigureServices(s => { s.AddSingleton(_appSessions); })
                .ConfigureServices(s => { s.AddSingleton(_resourceProvider); })
                .ConfigureServices(s => { s.AddSingleton<IStartup>(_startup); });

            if (Environment.OSVersion.Platform == PlatformID.Win32NT && _options.UseNtlmAuthentication)
            {
                _logger.LogInformation("KestrelHost.Start: Using HttpSys mode (NTLM authentication)");
                builder = WindowsHosting.UseNtlmAuthentication(builder, httpSysAddress);
            }
            else
            {
                _logger.LogInformation("KestrelHost.Start: Using Kestrel/Sockets mode");
                builder = builder
                    .UseSockets()
                    .UseKestrel(options =>
                    {
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
            }

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                _logger.LogInformation("KestrelHost.Start: Enable hosting in IIS");
                builder = WindowsHosting.EnableIIS(builder);
            }

            _webApp?.Dispose();
            _webApp = builder.Build();

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

            var serverAddressesFeature = _webApp.ServerFeatures.Get<IServerAddressesFeature>();
            if (serverAddressesFeature != null)
            {
                foreach (var address in serverAddressesFeature.Addresses)
                {
                    _logger.LogInformation("KestrelHost.Start: Listening on {Address}",
                        address.Replace("0.0.0.0", "127.0.0.1"));
                }
            }

            _logger.LogInformation("KestrelHost.Start: succeeded");
        }
        catch (Exception ex)
        {
            if ((ex.InnerException is HttpListenerException {ErrorCode: 5}))
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
            _logger.LogInformation("KestrelHost.Terminate: WebApp...");
            _webApp?.Dispose();
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