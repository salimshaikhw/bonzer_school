using System.Text.Json;
using API.Logging;
using Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Serilog.Context;

namespace API.ExceptionHandling;

/// <summary>
/// The single place where a failed request is logged and answered (registered with AddExceptionHandler,
/// run by UseExceptionHandler). Services and repositories should throw, not log-and-throw.
///
/// Log entry: "POST /api/student failed with 409 UniqueViolation: ... (reference ...)", with the tenant,
/// user, correlation id and error reference as properties; 5xx include the exception (Datadog Error
/// Tracking), 4xx are warnings.
///
/// Response: <see cref="ExceptionSummary"/> (field names unchanged for the client). The user sees a safe
/// message and a reference id; internals are only shown in Development.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = null };

    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var error = ExceptionClassifier.Classify(exception, context.RequestAborted.IsCancellationRequested);
        var reference = ExceptionClassifier.Chain(exception).OfType<HCBException>().FirstOrDefault()?.ReferenceId is { } id && id != Guid.Empty
            ? id.ToString()
            : Guid.NewGuid().ToString();

        Log(context, exception, error, reference);

        if (context.Response.HasStarted)
            return false; // nothing can be written any more; the server aborts the response

        context.Response.Clear();
        context.Response.StatusCode = error.StatusCode;
        if (error.StatusCode == ExceptionClassifier.ClientClosedRequest)
            return true;

        var summary = new ExceptionSummary
        {
            UserMessage = error.ClientMessage ?? "There was an error processing your request.",
            ValidationMessage = error.StatusCode < 500 ? error.ClientMessage : null,
            SupportMessages = _environment.IsDevelopment()
                ? string.Join(" --> ", ExceptionClassifier.Chain(exception).Select(e => $"{e.GetType().Name}: {e.Message}"))
                : $"If the problem persists, contact support with reference {reference}.",
            ReferenceId = reference,
            StatusCode = error.StatusCode,
            ErrorCode = error.ErrorCode,
            CorrelationId = context.TraceIdentifier,
            Errors = error.Errors,
        };

        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(context.Response.Body, summary, JsonOptions, cancellationToken);
        return true;
    }

    private void Log(HttpContext context, Exception exception, ErrorClassification error, string reference)
    {
        // The request's tenant/user properties were removed from the log context when the exception left
        // RequestLogContextMiddleware, so they are added again here.
        var pushed = RequestLogContext.Collect(context)
            .Select(p => LogContext.PushProperty(p.Key, p.Value))
            .ToList();
        try
        {
            var message = ExceptionClassifier.CleanMessage(
                ExceptionClassifier.Chain(exception).LastOrDefault()?.Message ?? exception.Message);

            // Errors carry the exception (stack trace, Datadog Error Tracking); expected 4xx do not need it.
            _logger.Log(
                error.LogLevel,
                error.LogLevel >= LogLevel.Error ? exception : null,
                "{RequestMethod} {RequestPath} failed with {StatusCode} {" + LogProperties.ErrorCode + "}: {ErrorMessage} (reference {" + LogProperties.ErrorReference + "})",
                context.Request.Method,
                context.Request.Path.Value,
                error.StatusCode,
                error.ErrorCode,
                error.ClientMessage ?? message,
                reference);
        }
        finally
        {
            for (var i = pushed.Count - 1; i >= 0; i--)
                pushed[i].Dispose();
        }
    }
}
