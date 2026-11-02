using Serilog.Core;
using Serilog.Events;

namespace API.Logging;

/// <summary>
/// Adds Datadog's standard attributes to every event:
///   status                                    -> log level as Datadog names it (info, warning, error, ...)
///   error.kind / error.message / error.stack  -> for events with an exception; Datadog Error Tracking
///                                                groups errors on these.
/// </summary>
public sealed class DatadogEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(LogProperties.DatadogStatus, Status(logEvent.Level)));

        if (logEvent.Exception is not { } exception)
            return;

        // The innermost exception is usually the cause (services wrap database errors).
        var root = exception;
        while (root.InnerException != null)
            root = root.InnerException;

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(LogProperties.DatadogErrorKind, root.GetType().FullName));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(LogProperties.DatadogErrorMessage,
            ReferenceEquals(root, exception) ? exception.Message : $"{exception.Message} --> {root.Message}"));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(LogProperties.DatadogErrorStack, exception.ToString()));
    }

    private static string Status(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => "trace",
        LogEventLevel.Debug => "debug",
        LogEventLevel.Information => "info",
        LogEventLevel.Warning => "warning",
        LogEventLevel.Error => "error",
        _ => "critical"
    };
}
