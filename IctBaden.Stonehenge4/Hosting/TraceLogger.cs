using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace IctBaden.Stonehenge.Hosting;

internal class TraceLogger(string context) : ILogger
{
    private string _scopeContext = string.Empty;

    private sealed class LogScope : IDisposable
    {
        private readonly TraceLogger _logger;

        public LogScope(TraceLogger logger, string context)
        {
            _logger = logger;
            _logger._scopeContext = context;
        }

        public void Dispose()
        {
            _logger._scopeContext = string.Empty;
        }
    }


    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        var ctx = state.ToString() ?? string.Empty; 
        return new LogScope(this, ctx);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception, string> formatter)
    {
        try
        {
            var logLine = context + ": ";
            if (!string.IsNullOrEmpty(_scopeContext))
            {
                logLine += _scopeContext + " ";
            }

            if (state != null)
            {
                logLine += state.ToString();
            }
            if (exception != null)
            {
                logLine += ", " + exception.Message;
            }

            switch (logLevel)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                case LogLevel.Information:
                    Trace.TraceInformation(logLine);
                    break;
                case LogLevel.Warning:
                    Trace.TraceWarning(logLine);
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    Trace.TraceError(logLine);
                    break;
                default:
                    return;
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.Message);
            Debugger.Break();
        }
    }
}