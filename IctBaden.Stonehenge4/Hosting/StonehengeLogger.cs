using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IctBaden.Stonehenge.Hosting;

[SuppressMessage("Design", "MA0069:Non-constant static fields should not be visible")]
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