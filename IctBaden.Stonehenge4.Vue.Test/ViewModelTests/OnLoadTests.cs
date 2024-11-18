using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IctBaden.Stonehenge.Vue.Test.ViewModelTests;

[SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP014:Use a single instance of HttpClient")]
public sealed class OnLoadTests : IDisposable
{
    private readonly ILogger _logger = StonehengeLogger.DefaultLogger;
    private readonly VueTestApp _app = new();

    public void Dispose()
    {
        _app.Dispose();
    }

    [Fact]
    public async Task OnLoadShouldBeCalledForStartVmAfterFirstCall()
    {
        var response = string.Empty;

        try
        {
            using var client = new RedirectableHttpClient();
            response = await client.DownloadStringWithSession(_app.BaseUrl + "/ViewModel/StartVm");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(OnLoadShouldBeCalledForStartVmAfterFirstCall));
        }

        Assert.NotNull(response);
        Assert.Equal(1, _app.Data.StartVmOnLoadCalled);
        Assert.Empty(_app.Data.StartVmParameters);
    }
        
}