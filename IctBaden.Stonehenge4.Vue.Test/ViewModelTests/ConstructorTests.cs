using System;
using IctBaden.Framework.Logging;
using IctBaden.Framework.Tron;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IctBaden.Stonehenge.Vue.Test.ViewModelTests;

public sealed class ConstructorTests : IDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private string _log = string.Empty;
    
    private readonly VueTestApp _app;

    public ConstructorTests()
    {
        _loggerFactory = Logger
            .CreateConsoleAndTronFactory(Logger.GetLogConfiguration(LogLevel.Trace));
        var logger = _loggerFactory.CreateLogger("TEST");
        TronTrace.OnPrint += log => _log += Environment.NewLine + log;
        
        var testVmAssembly = typeof(TestVm.Program).Assembly;
        _app = new VueTestApp(testVmAssembly, logger);
    }

    public void Dispose()
    {
        _app.Dispose();
        _loggerFactory.Dispose();
    }

    [Fact]
    public void ProtectedViewModelConstructorShouldBeLoggedAsError()
    {
        Assert.Contains("Failed to create ViewModel", _log, StringComparison.OrdinalIgnoreCase);
    }
    
}