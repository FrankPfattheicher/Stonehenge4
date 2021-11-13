using System;
using System.Reflection;
using IctBaden.Stonehenge4.Hosting;
using IctBaden.Stonehenge4.Kestrel;
using IctBaden.Stonehenge4.Resources;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace IctBaden.Stonehenge4.Vue.Test
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
