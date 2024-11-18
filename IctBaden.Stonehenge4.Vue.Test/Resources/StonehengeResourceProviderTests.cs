using System;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IctBaden.Stonehenge.Vue.Test.Resources;

public sealed class StonehengeResourceProviderTests : IDisposable
{
    private readonly ILogger _logger = StonehengeLogger.DefaultLogger;
    private readonly VueTestApp _app = new();

    public void Dispose()
    {
        _app.Dispose();
    }

    [Fact]
    public async Task PostRequestShouldBeHandled()
    {
        var response = string.Empty;
        try
        {
#pragma warning disable IDISP014
            using var client = new RedirectableHttpClient();
#pragma warning restore IDISP014
            response = await client.DownloadStringWithSession(_app.BaseUrl + "/app.js");
            response = await client.Post(_app.BaseUrl + "/user/request?p1=11&p2=22", "{}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(PostRequestShouldBeHandled));
        }

        Assert.NotNull(response);
        Assert.Contains("11", response, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("22", response, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("POST", response, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PutRequestShouldBeHandled()
    {
        var response = string.Empty;
        try
        {
            using var client = new RedirectableHttpClient();
            response = await client.DownloadStringWithSession(_app.BaseUrl + "/app.js");
            response = await client.Put(_app.BaseUrl + "/user/request?p1=11&p2=22", "{}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(PostRequestShouldBeHandled));
        }

        Assert.NotNull(response);
        Assert.Contains("11", response, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("22", response, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PUT", response, StringComparison.OrdinalIgnoreCase);
    }
    
    [Fact]
    public async Task DeleteRequestShouldBeHandled()
    {
        var response = string.Empty;
        try
        {
            using var client = new RedirectableHttpClient();
            response = await client.DownloadStringWithSession(_app.BaseUrl + "/app.js");
            response = await client.Delete(_app.BaseUrl + "/user/request?p1=11&p2=22");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(PostRequestShouldBeHandled));
        }

        Assert.NotNull(response);
        Assert.Contains("11", response, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("22", response, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DELETE", response, StringComparison.OrdinalIgnoreCase);
    }

}
