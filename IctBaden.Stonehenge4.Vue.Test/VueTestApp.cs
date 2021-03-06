using System;
using System.Reflection;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Kestrel;
using IctBaden.Stonehenge.Resources;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace IctBaden.Stonehenge.Vue.Test
{
    public class VueTestApp : IDisposable
    {
        public string BaseUrl => _server?.BaseUrl;

        private readonly IStonehengeHost _server;

        public readonly VueTestData Data = new VueTestData();

        public VueTestApp(Assembly appAssembly = null)
        {
            var vue = new VueResourceProvider(StonehengeLogger.DefaultLogger);
            var loader = appAssembly != null
                ? StonehengeResourceLoader.CreateDefaultLoader(StonehengeLogger.DefaultLogger, vue, appAssembly)
                : StonehengeResourceLoader.CreateDefaultLoader(StonehengeLogger.DefaultLogger, vue);
            loader.Services.AddService(typeof(VueTestData), Data);
            _server = new KestrelHost(loader);
            _server.Start("localhost");
        }

        public void Dispose()
        {
            _server?.Terminate();
        }
        
    }
}
