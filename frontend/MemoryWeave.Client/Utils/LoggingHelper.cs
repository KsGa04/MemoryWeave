namespace MemoryWeave.Client.Utils;

using System;
using System.Collections.Generic;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Microsoft.Extensions.Logging;

/// <summary>
/// Logging configuration helper
/// </summary>
public static class LoggingHelper
{
    private static Logger? _serilogLogger;

    /// <summary>
    /// Configure Serilog logger
    /// </summary>
    public static ILoggerFactory CreateLoggerFactory(string logLevel = "Information")
    {
        var minLevel = Enum.Parse<LogEventLevel>(logLevel, ignoreCase: true);

        _serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Is(minLevel)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
            )
            .WriteTo.File(
                "logs/memoryweave-.txt",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
            )
            .CreateLogger();

        Log.Logger = _serilogLogger;

        return LoggerFactory.Create(builder =>
        {
            builder
                .ClearProviders()
                .AddSerilog(_serilogLogger);
        });
    }

    /// <summary>
    /// Get Serilog logger
    /// </summary>
    public static ILogger GetLogger<T>()
    {
        return Log.ForContext<T>();
    }

    /// <summary>
    /// Dispose logger
    /// </summary>
    public static void Shutdown()
    {
        Log.CloseAndFlush();
    }
}
