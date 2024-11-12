using System;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IctBaden.Stonehenge.Vue.Test.ViewModelTests;

public sealed class SessionTests : IDisposable
{
    private readonly ILogger _logger = StonehengeLogger.DefaultLogger;
    private readonly VueTestApp _app = new();

    public void Dispose()
    {
        _app.Dispose();
    }

    [Fact]
    public async Task RequestWithSessionShouldGetSessionByHeader()
    {
        var response = string.Empty;
        using var client = new RedirectableHttpClient();

        try
        {
            // ReSharper disable once ConvertToUsingDeclaration
            response = await client.DownloadStringWithSession(_app.BaseUrl + "/ViewModel/StartVm");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(RequestWithSessionShouldGetSessionByHeader));
        }

        Assert.NotNull(response);
        Assert.Equal("Header", client.SessionIdBy);
    }
}