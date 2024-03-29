using System;
using System.ComponentModel.Design;
using System.Reflection;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Kestrel;
using IctBaden.Stonehenge.Resources;
using IctBaden.Stonehenge.Vue;
using Microsoft.Extensions.Logging;

// ReSharper disable MemberCanBePrivate.Global

namespace IctBaden.Stonehenge.App;

public sealed class StonehengeUi : IDisposable
{
    public IStonehengeHost? Server { get; private set; }
    public readonly ILogger Logger;

#pragma warning disable IDISP002
    private readonly StonehengeResourceLoader _loader;
#pragma warning restore IDISP002
    private readonly StonehengeHostOptions _options;

    // ReSharper disable once UnusedMember.Global
    public StonehengeUi(string title)
        : this(StonehengeLogger.DefaultFactory.CreateLogger("stonehenge"),
            new StonehengeHostOptions
            {
                Title = title,
                ServerPushMode = ServerPushModes.LongPolling,
                PollIntervalSec = 5
            },
            Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly())
    {
    }

    public StonehengeUi(ILogger logger, StonehengeHostOptions options)
        : this(logger, options, Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly())
    {
    }

    public StonehengeUi(ILogger logger, StonehengeHostOptions options, Assembly appAssembly)
    {
        _options = options;
        StonehengeLogger.DefaultLevel = LogLevel.Trace;
        Logger = logger;

#pragma warning disable IDISP001
        var vue = new VueResourceProvider(Logger);
        _loader = StonehengeResourceLoader.CreateDefaultLoader(Logger, vue, appAssembly);
#pragma warning restore IDISP001
    }

    public void AddResourceAssembly(Assembly assembly)
    {
        _loader.AddResourceAssembly(assembly);
    }

    public bool Start() => Start(0, false);
    public bool Start(int port, bool publicReachable)
    {
        var host = publicReachable ? "*" : "localhost";
#pragma warning disable IDISP003
        Server = new KestrelHost(_loader, _options);
#pragma warning restore IDISP003
        return Server.Start(host, port);
    }

    public void Dispose() => Server?.Terminate();

    public ServiceContainer Services => _loader.Services;

}