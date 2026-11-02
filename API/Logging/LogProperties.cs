namespace API.Logging;

/// <summary>
/// Names of the structured properties every log event of a request carries. They become attributes in
/// Datadog, so keep them stable: dashboards, monitors and saved views filter on them.
/// </summary>
public static class LogProperties
{
    /// <summary>Id of the request, from the X-Correlation-ID header or generated. Returned to the client.</summary>
    public const string CorrelationId = "CorrelationId";

    /// <summary>Catalog tenants.id of the school group the request works on.</summary>
    public const string TenantId = "TenantId";

    /// <summary>Tenant identifier, e.g. "demo".</summary>
    public const string Tenant = "Tenant";

    /// <summary>Identity user id, or the OAuth client id for machine clients.</summary>
    public const string UserId = "UserId";

    /// <summary>Reference shown to the user in an error response, so support can find the log entry.</summary>
    public const string ErrorReference = "ErrorReference";

    /// <summary>Stable error category, e.g. "Validation", "UniqueViolation", "Unexpected".</summary>
    public const string ErrorCode = "ErrorCode";

    // Datadog standard attributes. With these, error logs appear in Datadog Error Tracking.
    public const string DatadogErrorKind = "error.kind";
    public const string DatadogErrorMessage = "error.message";
    public const string DatadogErrorStack = "error.stack";
    public const string DatadogStatus = "status";
}
