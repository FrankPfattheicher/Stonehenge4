using System;
using System.Diagnostics;
using System.Threading;
using IctBaden.Framework.Logging;
using IctBaden.Stonehenge.Client;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Kestrel;
using IctBaden.Stonehenge.Resources;
using IctBaden.Stonehenge.Extension;
using IctBaden.Stonehenge4.Forms;
using Microsoft.Extensions.Logging;

namespace IctBaden.Stonehenge.Vue.SampleCore;

internal static class Program
{
    private static IStonehengeHost? _server;

    // ReSharper disable once MemberCanBePrivate.Global
    public static readonly ILoggerFactory LoggerFactory = 
        Logger.CreateConsoleAndTronFactory(Logger.GetLogConfiguration(LogLevel.Trace));

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    private static void Main()
    {
        Trace.Listeners.Add(new System.Diagnostics.ConsoleTraceListener());
        StonehengeLogger.DefaultLevel = LogLevel.Trace;
        var logger = LoggerFactory.CreateLogger("stonehenge");

        Console.WriteLine();
        Console.WriteLine("Stonehenge 4 Sample");
        Console.WriteLine();
        logger.LogInformation("Vue.SampleCore started");

        // ReSharper disable once RedundantAssignment
        KeycloakAuthenticationOptions? keycloak = null;
        // keycloak = new KeycloakAuthenticationOptions
        // {
        //     ClientId = "frontend",
        //     Realm = "liva-pms",
        //     AuthUrl = "https://auth.liva-cloud.com"
        // };

        UserContentLinks.AddStyleSheet(string.Empty, "theme/theme{{theme}}.css");
            
        // select hosting options
        var options = new StonehengeHostOptions
        {
            Title = "VueSample",

            ServerPushMode = ServerPushModes.LongPolling,
            PollIntervalSec = 10,
            HandleWindowResized = true,
            CustomMiddleware = [nameof(StonehengeRawContent)],
            UseClientLocale = true,
            UseNtlmAuthentication = false,
            UseKeycloakAuthentication = keycloak
            // SslCertificatePath = Path.Combine(StonehengeApplication.BaseDirectory, "stonehenge.pfx"),
            // SslCertificatePassword = "test"
        };

            // select hosting options
            var options = new StonehengeHostOptions
            {
                Title = "VueSample",

                ServerPushMode = ServerPushModes.LongPolling,
                PollIntervalSec = 10,
                HandleWindowResized = true,
                UseKeycloakAuthentication = new KeycloakAuthenticationOptions
                {
                    ClientId = "frontend",
                    Realm = "liva-production",
                    AuthUrl = "https://portal.liva-aws.com/auth"
                }
                // SslCertificatePath = Path.Combine(StonehengeApplication.BaseDirectory, "stonehenge.pfx"),
                // SslCertificatePassword = "test"
            };

            // Select client framework
            Console.WriteLine(@"Using client framework vue");
            var vue = new VueResourceProvider(logger);
            var loader = StonehengeResourceLoader.CreateDefaultLoader(logger, vue);
            loader.AddResourceAssembly(typeof(ChartsC3).Assembly);
            loader.AddResourceAssembly(typeof(AppDialog).Assembly);
            loader.Services.AddService(typeof(ILogger), logger);
            
        // Select hosting technology
        logger.LogInformation("Using Kestrel hosting");
        _server = new KestrelHost(loader, options);

        logger.LogInformation("Starting server");
        using var terminate = new AutoResetEvent(false);
        // ReSharper disable once AccessToDisposedClosure
        Console.CancelKeyPress += (_, _) => { terminate.Set(); };

        var host = Environment.CommandLine.Contains("/localhost") ? "localhost" : "*";
        if (_server.Start(host, 32000))
        {
            if (Environment.CommandLine.Contains("/window"))
            {
                using var wnd = new HostWindow(logger, _server.BaseUrl, options.Title, new Point(1400, 1100));
                if (!wnd.Open())
                {
                    logger.LogError("Failed to open main window");
                    terminate.Set();
                }
            }
            else
            {
                terminate.WaitOne();
            }

            logger.LogInformation("Server terminated");
        }
        else
        {
            logger.LogError("Failed to start server on: {BaseUrl}", _server.BaseUrl);
        }

#pragma warning disable 0162
        // ReSharper disable once HeuristicUnreachableCode
        _server.Terminate();

        logger.LogInformation("Exit sample app");
        Environment.Exit(0);
    }
}