namespace API.Logging;

/// <summary>
/// "AppLogging" configuration section: where logs are written. Levels are set in the "Serilog" section
/// (MinimumLevel / Override), so they can be changed per environment without a code change.
/// </summary>
public sealed class LoggingOptions
{
    public const string SectionName = "AppLogging";

    /// <summary>Service name in every log event and in Datadog ("service" tag).</summary>
    public string ServiceName { get; set; } = "bonzer-school-api";

    /// <summary>
    /// Console output as JSON (one event per line) instead of text. Null: JSON everywhere except Development.
    /// JSON is what Azure App Service log streams, containers and the Datadog Agent should collect.
    /// </summary>
    public bool? JsonConsole { get; set; }

    /// <summary>Rolling JSON log file, relative to the app folder. Empty disables the file.</summary>
    public string? FilePath { get; set; } = "Logs/bonzer-api-.json";

    public int RetainedFileCount { get; set; } = 14;

    /// <summary>A file rolls over early once it reaches this size.</summary>
    public long FileSizeLimitBytes { get; set; } = 100L * 1024 * 1024;

    /// <summary>Requests slower than this are logged as warnings.</summary>
    public int SlowRequestThresholdMs { get; set; } = 3000;
}

/// <summary>
/// "Datadog" configuration section: ships logs straight to Datadog's HTTP intake. Leave it disabled when
/// a Datadog Agent collects the console / file output instead. Keep the ApiKey in User Secrets or Azure
/// configuration (Datadog__ApiKey), never in appsettings.
/// </summary>
public sealed class DatadogOptions
{
    public const string SectionName = "Datadog";

    public bool Enabled { get; set; }

    public string? ApiKey { get; set; }

    /// <summary>Datadog site of the account: datadoghq.com, datadoghq.eu, us3.datadoghq.com, us5.datadoghq.com, ap1.datadoghq.com.</summary>
    public string Site { get; set; } = "datadoghq.com";

    /// <summary>Lowest level sent to Datadog (lower levels still go to console and file).</summary>
    public string MinimumLevel { get; set; } = "Information";

    /// <summary>Extra tags, e.g. ["team:erp", "region:in"]. env and version are added automatically.</summary>
    public string[] Tags { get; set; } = Array.Empty<string>();
}
