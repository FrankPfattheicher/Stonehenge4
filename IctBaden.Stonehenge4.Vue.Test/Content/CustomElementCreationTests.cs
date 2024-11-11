using System;
using System.Diagnostics.CodeAnalysis;
using IctBaden.Stonehenge.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IctBaden.Stonehenge.Vue.Test.Content;

[SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP014:Use a single instance of HttpClient")]
public sealed class CustomElementCreationTests : IDisposable
{
    private readonly ILogger _logger = StonehengeLogger.DefaultLogger;
    private readonly VueTestApp _app;
    private readonly string _response;

    public CustomElementCreationTests()
    {
        _app = new VueTestApp();
            
        _response = string.Empty;
        using var client = new RedirectableHttpClient();
        try
        {
            // ReSharper disable once ConvertToUsingDeclaration
            _response = client.DownloadStringWithSession(_app.BaseUrl + "/app.js").Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(CustomElementCreationTests));
        }

    }

    public void Dispose()
    {
        _app.Dispose();
    }

    [Fact]
    public void AppJsShouldContainEmbeddedResourceCustomElement1Definition()
    {
        Assert.Contains("'CustElem1'", _response, StringComparison.Ordinal);
    }

    [Fact]
    public void AppJsShouldContainEmbeddedResourceCustomElement2Definition()
    {
        Assert.Contains("'CustElem2'", _response, StringComparison.Ordinal);
    }

    [Fact]
    public void AppJsShouldContainFileBasedCustomElement3Definition()
    {
        Assert.Contains("'CustElem2'", _response, StringComparison.Ordinal);
    }

    [Fact]
    public void AppJsShouldContainCustomElement1Parameter()
    {
        Assert.Contains("['one']", _response, StringComparison.Ordinal);
    }

    [Fact]
    public void AppJsShouldContainCustomElement2ParameterLists()
    {
        Assert.Contains("['one','two']", _response, StringComparison.Ordinal);
    }
        
    [Fact]
    public void AppJsShouldContainCustomElement3ParameterLists()
    {
        Assert.Contains("['one','two','three']", _response, StringComparison.Ordinal);
    }
        
}
