using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.Vue.TestApp2.ViewModels;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IctBaden.Stonehenge.Vue.Test.Content;

[SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP014:Use a single instance of HttpClient")]
public sealed class IndexTests : IDisposable
{
    private readonly ILogger _logger = StonehengeLogger.DefaultLogger;
    private readonly VueTestApp _app1 = new();
    private readonly VueTestApp _app2 = new(typeof(SecondAppVm).Assembly);

    public void Dispose()
    {
        _app1.Dispose();
        _app2.Dispose();
    }

    [Fact]
    public async Task AppWithoutPrivateIndexShouldGetIndexFromFramework()
    {
        var response = string.Empty;

        // app1 - no private index.html
        using var client = new RedirectableHttpClient();
        try
        {
            // ReSharper disable once ConvertToUsingDeclaration
            response = await client.DownloadStringWithSession(_app1.BaseUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(AppWithoutPrivateIndexShouldGetIndexFromFramework));
        }

        Assert.NotNull(response);
        Assert.Contains("<!--IctBaden.Stonehenge.Vue.index.html-->", response, StringComparison.Ordinal);
        Assert.DoesNotContain("<!--IctBaden.Stonehenge.Vue.TestApp2.index.html-->", response, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AppWithPrivateIndexShouldGetPrivateIndex()
    {
        var response = string.Empty;

        // app2 - with private index.html
        try
        {
            // ReSharper disable once ConvertToUsingDeclaration
            using (var client = new RedirectableHttpClient())
            {
                response = await client.DownloadStringWithSession(_app2.BaseUrl);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(AppWithPrivateIndexShouldGetPrivateIndex));
        }

        Assert.NotNull(response);
        Assert.Contains("<!--IctBaden.Stonehenge.Vue.TestApp2.index.html-->", response, StringComparison.Ordinal);
        Assert.DoesNotContain("<!--IctBaden.Stonehenge.Vue.index.html-->", response, StringComparison.Ordinal);
    }

}