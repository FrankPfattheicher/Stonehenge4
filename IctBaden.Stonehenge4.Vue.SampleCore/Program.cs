using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using IctBaden.Framework.Logging;
using IctBaden.Stonehenge.Client;
using IctBaden.Stonehenge.Extension;
using IctBaden.Stonehenge.Forms;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Kestrel;
using IctBaden.Stonehenge.Resources;
using Microsoft.Extensions.Logging;

namespace IctBaden.Stonehenge.Vue.SampleCore;

internal static class Program
{
    private static IStonehengeHost? _server;

    // ReSharper disable once MemberCanBePrivate.Global
    public static readonly ILoggerFactory LoggerFactory = 
        Logger.CreateConsoleAndTronFactory(Logger.GetLogConfiguration(LogLevel.Debug));

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [SuppressMessage("Design", "MA0051:Method is too long")]
    private static void Main()
    {
        Trace.Listeners.Add(new System.Diagnostics.ConsoleTraceListener());
        StonehengeLogger.DefaultLevel = LogLevel.Trace;
        var logger = LoggerFactory.CreateLogger("stonehenge");

        Console.WriteLine();
        Console.WriteLine(@"Stonehenge 4 Sample");
        Console.WriteLine();
        logger.LogInformation("Vue.SampleCore started");

        // ReSharper disable once RedundantAssignment
        KeycloakAuthenticationOptions? keycloak = null;
        keycloak = new KeycloakAuthenticationOptions
        {
            ClientId = "frontend",
            Realm = "liva-pms",
            AuthUrl = "https://auth.liva-cloud.com"
        };
        
        // ReSharper disable once RedundantAssignment
        var cert = "stonehenge.pfx";
        cert = string.Empty;
        
        UserContentLinks.AddStyleSheet(string.Empty, "theme/theme{{theme}}.css");
            
        // select hosting options
        var options = new StonehengeHostOptions
        {
            Title = "VueSample",

            ServerPushMode = ServerPushModes.ServerSentEvents,
            PollIntervalSec = 10,
            HandleWindowResized = true,
            CustomMiddleware = [nameof(StonehengeRawContent)],
            UseClientLocale = false,
            //DefaultLocale = "en-US",
            UseNtlmAuthentication = false,
            UseKeycloakAuthentication = keycloak,
            UseBasicAuth = false,
            SslCertificatePath = Path.Combine(StonehengeApplication.BaseDirectory, cert),
            SslCertificatePassword = "stonehenge"
        };

        // Select client framework
        logger.LogInformation("Using client framework VUE");
#pragma warning disable IDISP001
        var vue = new VueResourceProvider(logger);
        var loader = StonehengeResourceLoader.CreateDefaultLoader(logger, vue);
#pragma warning restore IDISP001
        loader.AddResourceAssembly(typeof(TreeView).Assembly);
        loader.AddResourceAssembly(typeof(ChartsC3).Assembly);
        loader.AddResourceAssembly(typeof(AppDialog).Assembly);
        loader.AddResourceAssembly(typeof(FormControls).Assembly);
        loader.AddResourceAssembly(typeof(Mermaid).Assembly);
        //loader.AddResourceAssembly(typeof(SyntaxHighlight).Assembly);
        loader.Services.AddService(typeof(ILogger), logger);
            
        // Select hosting technology
        logger.LogInformation("Using Kestrel hosting");
        _server = new KestrelHost(loader, options);

        logger.LogInformation("Starting server");
        using var terminate = new AutoResetEvent(false);
        // ReSharper disable once AccessToDisposedClosure
        Console.CancelKeyPress += (_, _) => { terminate.Set(); };

        var window = Environment.CommandLine.Contains("/window");
        var host = Environment.CommandLine.Contains("/localhost") ? "localhost" : "*";
        if (_server.Start(host, window ? 0 : 32000))
        {
            if (window)
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