using System;
using System.Reflection;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Kestrel;
using IctBaden.Stonehenge.Resources;
using IctBaden.Stonehenge.Test.Tools;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace IctBaden.Stonehenge.Vue.Test;

public sealed class VueTestApp : IDisposable
{
    public string BaseUrl => Server.BaseUrl;

    public readonly IStonehengeHost Server;

    public readonly VueTestData Data = new();

    public VueTestApp(Assembly? appAssembly = null)
    {
#pragma warning disable IDISP001
        var vue = new VueResourceProvider(StonehengeLogger.DefaultLogger);
        var loader = appAssembly != null
            ? StonehengeResourceLoader.CreateDefaultLoader(StonehengeLogger.DefaultLogger, vue, appAssembly)
            : StonehengeResourceLoader.CreateDefaultLoader(StonehengeLogger.DefaultLogger, vue);
#pragma warning restore IDISP001
        loader.Providers.Add(new TestResourceLoader("none"));
        loader.Services.AddService(typeof(VueTestData), Data);
        Server = new KestrelHost(loader);
        Server.Start("localhost");
    }

    public void Dispose()
    {
        Server.Terminate();
    }
        
}