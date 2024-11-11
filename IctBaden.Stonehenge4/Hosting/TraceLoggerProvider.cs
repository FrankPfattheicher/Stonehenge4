using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace IctBaden.Stonehenge.Hosting;

internal sealed class TraceLoggerProvider(string context) : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, TraceLogger> _loggers = new(System.StringComparer.Ordinal);

    public ILogger CreateLogger(string categoryName) => _loggers
        .GetOrAdd(categoryName, _ => new TraceLogger(context));

    public void Dispose() => _loggers.Clear();
}