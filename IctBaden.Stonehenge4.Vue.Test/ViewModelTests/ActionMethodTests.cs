using System;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IctBaden.Stonehenge.Vue.Test.ViewModelTests;

public sealed class ActionMethodTests : IDisposable
{
    private readonly ILogger _logger = StonehengeLogger.DefaultLogger;
    private readonly VueTestApp _app = new();

    public void Dispose()
    {
        _app.Dispose();
    }

    [Fact]
    public async Task PostWithParameterShouldExecuteActionAndReturnParameter()
    {
        await ExecutePost("test1234");
    }
    
    [Fact]
    public async Task PostWithEmptyParameterShouldExecuteActionAndReturnEmptyParameter()
    {
        await ExecutePost("");
    }
    
    private async Task ExecutePost(string parameterValue)
    {
        var startVm = string.Empty;
        var response = string.Empty;

        try
        {
            // ReSharper disable once ConvertToUsingDeclaration
            using var client = new RedirectableHttpClient();
            startVm = await client.DownloadStringWithSession(_app.BaseUrl + "/ViewModel/StartVm").ConfigureAwait(false);
            response = await client.Post(_app.BaseUrl + "/ViewModel/StartVm/TestAction?parameter=" + parameterValue, string.Empty).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(PostWithParameterShouldExecuteActionAndReturnParameter));
        }

        Assert.NotNull(startVm);
        Assert.NotNull(response);
        
        Assert.Contains($"\"{parameterValue}\"", response, StringComparison.Ordinal);
    }

}