using System;
using System.Reflection;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Kestrel;
using IctBaden.Stonehenge.Resources;
using IctBaden.Stonehenge.Test.Tools;
using Microsoft.Extensions.Logging;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace IctBaden.Stonehenge.Vue.Test;

public sealed class VueTestApp : IDisposable
{
    public string BaseUrl => Server.BaseUrl;

    public readonly IStonehengeHost Server;

    public readonly VueTestData Data = new();
    private readonly StonehengeResourceLoader _loader;

    public VueTestApp(Assembly? appAssembly = null, ILogger? logger = null)
    {
        logger ??= StonehengeLogger.DefaultLogger;
        var vue = new VueResourceProvider(logger);
        _loader = appAssembly != null
            ? StonehengeResourceLoader.CreateDefaultLoader(StonehengeLogger.DefaultLogger, vue, appAssembly)
            : StonehengeResourceLoader.CreateDefaultLoader(StonehengeLogger.DefaultLogger, vue);

        _loader.Providers.Add(new TestResourceLoader("none"));
        _loader.Services.AddService(typeof(VueTestData), Data);
        _loader.Services.AddService(typeof(ILogger), logger);
        
        Server = new KestrelHost(_loader);
        Server.Start("localhost");
    }

    public void Dispose()
    {
        Server.Terminate();
        _loader.Dispose();
    }

}