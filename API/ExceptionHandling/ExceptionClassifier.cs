using Application.Common;
using Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace API.ExceptionHandling;

/// <summary>How a failed request is answered and logged.</summary>
/// <param name="StatusCode">HTTP status of the response.</param>
/// <param name="ErrorCode">Stable category for clients and Datadog facets.</param>
/// <param name="ClientMessage">Safe to show to the user; null for unexpected errors (details stay in the log).</param>
/// <param name="LogLevel">Client mistakes are warnings; server faults are errors.</param>
/// <param name="Errors">Field errors (property name -> messages), for validation failures.</param>
public sealed record ErrorClassification(
    int StatusCode,
    string ErrorCode,
    string? ClientMessage,
    LogLevel LogLevel,
    IDictionary<string, string[]>? Errors = null);

/// <summary>
/// Maps an exception to an <see cref="ErrorClassification"/>. Services wrap errors (e.g. a not-found inside
/// a "Database" BaseServiceException), so the whole InnerException chain is inspected: the first exception
/// with a client-facing meaning decides, otherwise the request is a 500.
/// </summary>
public static class ExceptionClassifier
{
    /// <summary>Not an HTTP standard code, but widely used (nginx) for "client closed the request".</summary>
    public const int ClientClosedRequest = 499;

    private static readonly string[] MessagePrefixes = { "Service Exception: ", "RepositoryException: " };

    public static ErrorClassification Classify(Exception exception, bool requestAborted)
    {
        if (requestAborted && Chain(exception).Any(e => e is OperationCanceledException))
            return new(ClientClosedRequest, "RequestCancelled", null, LogLevel.Information);

        foreach (var current in Chain(exception))
        {
            var classification = current switch
            {
                HCBException hcb => FromApplicationException(hcb),
                FluentValidation.ValidationException validation => FromValidation(validation),
                DbUpdateConcurrencyException => new(StatusCodes.Status409Conflict, "Concurrency",
                    "The record was changed by someone else. Reload it and try again.", LogLevel.Warning),
                PostgresException postgres => FromPostgres(postgres),
                NpgsqlException { IsTransient: true } or TimeoutException => new(StatusCodes.Status503ServiceUnavailable,
                    "DatabaseUnavailable", "The service is temporarily unavailable. Please try again in a moment.", LogLevel.Error),
                BadHttpRequestException badRequest => new(badRequest.StatusCode, "BadRequest",
                    "The request could not be read.", LogLevel.Warning),
                _ => null
            };
            if (classification != null)
                return classification;
        }

        // Plain .NET exceptions thrown directly by our code (not wrapped).
        return exception switch
        {
            ArgumentException => new(StatusCodes.Status400BadRequest, "BadRequest", "The request is not valid.", LogLevel.Warning),
            KeyNotFoundException => new(StatusCodes.Status404NotFound, "ItemNotFound", "The requested item was not found.", LogLevel.Warning),
            UnauthorizedAccessException => new(StatusCodes.Status401Unauthorized, "UnAuthorized", "You are not signed in.", LogLevel.Warning),
            NotImplementedException => new(StatusCodes.Status501NotImplemented, "NotImplemented", "This feature is not available yet.", LogLevel.Error),
            _ => new(StatusCodes.Status500InternalServerError, "Unexpected", null, LogLevel.Error)
        };
    }

    /// <summary>The exception and its inner exceptions, outermost first (AggregateException included).</summary>
    public static IEnumerable<Exception> Chain(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            yield return current;
            if (current is AggregateException { InnerExceptions.Count: > 1 } aggregate)
            {
                foreach (var inner in aggregate.InnerExceptions.Skip(1))
                foreach (var nested in Chain(inner))
                    yield return nested;
            }
        }
    }

    /// <summary>Exception message without the "Service Exception: " style prefixes added by our exception types.</summary>
    public static string CleanMessage(string message)
    {
        foreach (var prefix in MessagePrefixes)
            while (message.StartsWith(prefix, StringComparison.Ordinal))
                message = message[prefix.Length..];
        return message;
    }

    private static ErrorClassification? FromApplicationException(HCBException exception)
    {
        var message = CleanMessage(exception.Message);
        return exception.ExceptionCode switch
        {
            ExceptionCodes.Validation => new(StatusCodes.Status400BadRequest, "Validation", message, LogLevel.Warning),
            ExceptionCodes.ItemNotFound => new(StatusCodes.Status404NotFound, "ItemNotFound", message, LogLevel.Warning),
            ExceptionCodes.UnAuthorized => new(StatusCodes.Status403Forbidden, "UnAuthorized", message, LogLevel.Warning),
            ExceptionCodes.Restriction => new(StatusCodes.Status403Forbidden, "Restriction", message, LogLevel.Warning),
            ExceptionCodes.Operation => new(StatusCodes.Status409Conflict, "Operation", message, LogLevel.Warning),
            // Default / Database / Storage / Routing say "something failed" but not why: keep looking inside.
            _ => null
        };
    }

    private static ErrorClassification FromValidation(FluentValidation.ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => string.IsNullOrEmpty(e.PropertyName) ? "" : e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
        var message = errors.Count > 0
            ? string.Join(" ", errors.SelectMany(e => e.Value))
            : "The request is not valid.";
        return new(StatusCodes.Status400BadRequest, "Validation", message, LogLevel.Warning, errors);
    }

    /// <summary>Database rule violations are caused by the request data, so they are 4xx with a plain message.</summary>
    private static ErrorClassification FromPostgres(PostgresException exception) => exception.SqlState switch
    {
        PostgresErrorCodes.UniqueViolation => new(StatusCodes.Status409Conflict, "UniqueViolation",
            "A record with the same details already exists.", LogLevel.Warning),
        PostgresErrorCodes.ForeignKeyViolation when exception.MessageText.StartsWith("update or delete", StringComparison.OrdinalIgnoreCase)
            => new(StatusCodes.Status409Conflict, "ForeignKeyViolation",
                "This record is still used by other records and cannot be deleted.", LogLevel.Warning),
        PostgresErrorCodes.ForeignKeyViolation => new(StatusCodes.Status400BadRequest, "ForeignKeyViolation",
            "A related record does not exist.", LogLevel.Warning),
        PostgresErrorCodes.NotNullViolation => new(StatusCodes.Status400BadRequest, "Validation",
            $"'{exception.ColumnName}' is required.", LogLevel.Warning),
        PostgresErrorCodes.StringDataRightTruncation => new(StatusCodes.Status400BadRequest, "Validation",
            "A value is longer than allowed.", LogLevel.Warning),
        PostgresErrorCodes.CheckViolation => new(StatusCodes.Status400BadRequest, "Validation",
            "One of the values is not allowed.", LogLevel.Warning),
        // RAISE EXCEPTION in our database functions (TC workflow): the text is a business rule message.
        PostgresErrorCodes.RaiseException => new(StatusCodes.Status400BadRequest, "BusinessRule",
            exception.MessageText, LogLevel.Warning),
        PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected => new(StatusCodes.Status409Conflict,
            "Concurrency", "The record was changed by another request. Please try again.", LogLevel.Warning),
        PostgresErrorCodes.TooManyConnections or PostgresErrorCodes.AdminShutdown or PostgresErrorCodes.CannotConnectNow
            => new(StatusCodes.Status503ServiceUnavailable, "DatabaseUnavailable",
                "The service is temporarily unavailable. Please try again in a moment.", LogLevel.Error),
        _ => new(StatusCodes.Status500InternalServerError, "Database", null, LogLevel.Error)
    };
}
