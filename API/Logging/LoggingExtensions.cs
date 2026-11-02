using System.Reflection;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Datadog.Logs;
using Serilog.Templates;

namespace API.Logging;

public static class LoggingExtensions
{
    /// <summary>
    /// One JSON object per line, using Datadog's attribute names (timestamp, status, message, logger,
    /// error.*), plus every structured property of the event. Datadog, Azure and most log tools parse it
    /// without a custom pipeline.
    /// </summary>
    private static readonly ExpressionTemplate JsonFormatter = new(
        "{ {timestamp: UtcDateTime(@t), message: @m, message_template: @mt, logger: SourceContext, " +
        "trace_id: @tr, span_id: @sp, ..rest()} }\n");

    private const string TextTemplate =
        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{CorrelationSuffix}: {Message:lj}{NewLine}{Exception}";

    /// <summary>Logger used until the host is built, so startup failures are logged too.</summary>
    public static void CreateBootstrapLogger() =>
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateBootstrapLogger();

    /// <summary>
    /// Serilog as the only logging provider. Levels come from the "Serilog" section, outputs from
    /// "AppLogging" and "Datadog" (see <see cref="LoggingOptions"/>, <see cref="DatadogOptions"/>).
    /// </summary>
    public static WebApplicationBuilder AddBonzerLogging(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<LoggingOptions>(builder.Configuration.GetSection(LoggingOptions.SectionName));
        builder.Services.Configure<DatadogOptions>(builder.Configuration.GetSection(DatadogOptions.SectionName));

        builder.Logging.ClearProviders();
        builder.Host.UseSerilog((context, services, logger) =>
        {
            var options = context.Configuration.GetSection(LoggingOptions.SectionName).Get<LoggingOptions>() ?? new LoggingOptions();
            var datadog = context.Configuration.GetSection(DatadogOptions.SectionName).Get<DatadogOptions>() ?? new DatadogOptions();
            var environment = context.HostingEnvironment.EnvironmentName.ToLowerInvariant();
            var version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                          ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";

            logger
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                // GlobalExceptionHandler logs every failed request once, with its context; the framework's own
                // "unhandled exception" entry would be a second copy without tenant, user or reference.
                .MinimumLevel.Override("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware", LogEventLevel.Fatal)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("service", options.ServiceName)
                .Enrich.WithProperty("env", environment)
                .Enrich.WithProperty("version", version)
                .Enrich.WithProperty("host", Environment.MachineName)
                .Enrich.With<DatadogEnricher>()
                .Destructure.ToMaximumDepth(4)
                .Destructure.ToMaximumStringLength(2000)
                .Destructure.ToMaximumCollectionCount(50);

            var jsonConsole = options.JsonConsole ?? !context.HostingEnvironment.IsDevelopment();
            if (jsonConsole)
                logger.WriteTo.Console(JsonFormatter);
            else
                logger.Enrich.With<CorrelationSuffixEnricher>().WriteTo.Console(outputTemplate: TextTemplate);

            if (!string.IsNullOrWhiteSpace(options.FilePath))
            {
                logger.WriteTo.File(
                    JsonFormatter,
                    Path.Combine(AppContext.BaseDirectory, options.FilePath),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: options.RetainedFileCount,
                    fileSizeLimitBytes: options.FileSizeLimitBytes,
                    rollOnFileSizeLimit: true);
            }

            if (datadog.Enabled && !string.IsNullOrWhiteSpace(datadog.ApiKey))
            {
                var tags = datadog.Tags.Concat(new[] { $"env:{environment}", $"version:{version}" }).ToArray();
                logger.WriteTo.DatadogLogs(
                    datadog.ApiKey,
                    source: "csharp",
                    service: options.ServiceName,
                    host: Environment.MachineName,
                    tags: tags,
                    configuration: new DatadogConfiguration { Url = $"https://http-intake.logs.{datadog.Site}" },
                    logLevel: Enum.TryParse<LogEventLevel>(datadog.MinimumLevel, true, out var level) ? level : LogEventLevel.Information);
            }
        });

        return builder;
    }

    /// <summary>
    /// One summary event per request: "HTTP GET /api/student responded 200 in 12.3 ms", with tenant, user,
    /// client IP, user agent and endpoint. 4xx and 5xx are warnings (the exception itself is logged by
    /// GlobalExceptionHandler); slow requests are warnings; health checks and Swagger are not logged.
    /// </summary>
    public static IApplicationBuilder UseBonzerRequestLogging(this IApplicationBuilder app)
    {
        var slowMs = app.ApplicationServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<LoggingOptions>>().Value.SlowRequestThresholdMs;

        return app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0} ms";
            options.GetLevel = (context, elapsedMs, exception) =>
            {
                var path = context.Request.Path;
                if (path.StartsWithSegments("/health") || path.StartsWithSegments("/swagger"))
                    return LogEventLevel.Verbose;
                if (exception != null)
                    return LogEventLevel.Error;
                if (context.Response.StatusCode >= 400 || elapsedMs > slowMs)
                    return LogEventLevel.Warning;
                return LogEventLevel.Information;
            };
            options.EnrichDiagnosticContext = (diagnostics, context) =>
            {
                foreach (var (name, value) in RequestLogContext.Collect(context))
                    diagnostics.Set(name, value);
                diagnostics.Set("ClientIp", context.Connection.RemoteIpAddress?.ToString());
                diagnostics.Set("UserAgent", context.Request.Headers.UserAgent.ToString());
                diagnostics.Set("Endpoint", context.GetEndpoint()?.DisplayName);
                diagnostics.Set("ResponseLength", context.Response.ContentLength);
            };
        });
    }

    /// <summary>Text console only: shows " [correlation-id]" after the logger name when there is one.</summary>
    private sealed class CorrelationSuffixEnricher : Serilog.Core.ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, Serilog.Core.ILogEventPropertyFactory propertyFactory)
        {
            var suffix = logEvent.Properties.TryGetValue(LogProperties.CorrelationId, out var id)
                ? $" [{(id as ScalarValue)?.Value}]"
                : string.Empty;
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CorrelationSuffix", suffix));
        }
    }
}
