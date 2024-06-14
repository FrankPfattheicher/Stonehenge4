using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Resources;
using Xunit;

// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

namespace IctBaden.Stonehenge.Vue.Test.Client;

public sealed class ControllerCreationTests : IDisposable
{
    private readonly StonehengeHostOptions _options;
    private readonly AppSession _session;
    private readonly VueResourceProvider _vue;
    private readonly StonehengeResourceLoader _loader;
      
    public ControllerCreationTests()
    {
        _options = new StonehengeHostOptions();
        _vue = new VueResourceProvider(StonehengeLogger.DefaultLogger);
        _loader = StonehengeResourceLoader.CreateDefaultLoader(StonehengeLogger.DefaultLogger, _vue);
        _loader.InitProvider(_loader, _options);
        _loader.Services.AddService(typeof(DiDependency), new DiDependency());
        _session = new AppSession(_loader, _options, new AppSessions());
    }

    public void Dispose()
    {
        _session.Dispose();
        _vue.Dispose();
        _loader.Dispose();
    }

    [Fact]
    public void ProviderShouldGenerateStartComponent()
    {
        var resource = _loader.Get(_session, "start.js", new Dictionary<string, string>());
        Assert.NotNull(resource);
    }
        
    [Fact]
    public async Task StartComponentShouldHaveExpectedMembers()
    {
        var resource = await _loader.Get(_session, "start.js", new Dictionary<string, string>());
        Assert.NotNull(resource);
        Assert.Contains("VmPropInteger", resource.Text);
        Assert.Contains("VmPropText", resource.Text);
        Assert.Contains("VmPropList", resource.Text);
            
        Assert.DoesNotContain("//stonehengeProperties", resource.Text);
    }

    [Fact]
    public async Task ProviderShouldGenerateDiComponent()
    {
        var resource = await _loader.Get(_session, "dicomponent.js", new Dictionary<string, string>());
        Assert.NotNull(resource);
    }
        
    [Fact]
    public async Task DiComponentShouldHaveExpectedMembers()
    {
        var resource = await _loader.Get(_session, "dicomponent.js", new Dictionary<string, string>());
        Assert.NotNull(resource);
        Assert.Contains("VmPropInteger", resource.Text);
        Assert.Contains("VmPropText", resource.Text);
        Assert.Contains("VmPropList", resource.Text);
            
        Assert.DoesNotContain("//stonehengeProperties", resource.Text);
    }

}