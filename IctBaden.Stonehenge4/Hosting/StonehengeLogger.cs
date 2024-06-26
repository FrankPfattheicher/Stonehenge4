using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IctBaden.Stonehenge.Hosting
{
    public static class StonehengeLogger
    {
        private static ILoggerFactory? _defaultFactory;

        /// <summary>
        /// Tries to find static field of type ILoggerFactory in entry assembly.
        /// If not found return simple factory for console and tron output with default configuration.
        /// </summary>
        public static ILoggerFactory DefaultFactory
        {
            get
            {
                if (_defaultFactory != null) return _defaultFactory;

                // find static field of type ILoggerFactory in entry assembly
                var entry = Assembly.GetEntryAssembly();
                foreach (var entryType in entry!.DefinedTypes)
                {
                    var fieldInfo = entryType.DeclaredFields
                        .FirstOrDefault(f => f.FieldType == typeof(ILoggerFactory));
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                    var loggerFactory = (ILoggerFactory)fieldInfo?.GetValue(null);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                    if (loggerFactory == null) continue;

                    Trace.TraceInformation($"Using LoggerFactory '{fieldInfo?.Name}' of type '{entryType.Name}'.");
                    return loggerFactory;
                }

                Trace.TraceWarning($"No LoggerFactory found. Using console factory.");
                var emptyConfiguration = new ConfigurationBuilder().Build();
                _defaultFactory?.Dispose();
                _defaultFactory = CreateConsoleAndTronFactory(emptyConfiguration);
                return _defaultFactory;
            }
        }

        public static ILogger DefaultLogger => DefaultFactory.CreateLogger("stonehenge");

        public static LogLevel DefaultLevel = LogLevel.Warning;

        private static ILoggerFactory CreateConsoleAndTronFactory(IConfiguration configuration)
        {
            var minimumLogLevel = DefaultLevel;
            if (Enum.TryParse(typeof(LogLevel), configuration["LogLevel"], out var level) &&
                level is LogLevel cfgLogLevel)
            {
                minimumLogLevel = cfgLogLevel;
            }

            var loggerFactory = LoggerFactory.Create(builder =>
            {
#pragma warning disable IDISP004
                builder.AddProvider(new TraceLoggerProvider("stonehenge"));
                builder.SetMinimumLevel(minimumLogLevel);
#pragma warning restore IDISP004
            });
            return loggerFactory;
        }
    }


    internal class TraceLogger(string context) : ILogger
    {
        private string _scopeContext = "";

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
                _logger._scopeContext = "";
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

    internal sealed class TraceLoggerProvider(string context) : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, TraceLogger> _loggers =
            new ConcurrentDictionary<string, TraceLogger>();

        public ILogger CreateLogger(string categoryName) =>
            _loggers.GetOrAdd(categoryName, _ => new TraceLogger(context));

        public void Dispose() => _loggers.Clear();
    }
}