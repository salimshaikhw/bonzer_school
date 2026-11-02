using System.Diagnostics;
using API.Logging;
using Serilog.Context;

namespace API.Middlewares;

/// <summary>
/// Gives every request a correlation id: the caller's X-Correlation-ID header when it is a safe value,
/// otherwise the W3C trace id. It is added to every log event of the request, returned in the response
/// header and used as HttpContext.TraceIdentifier, so a user, the SPA and Datadog all see the same id.
/// Runs first in the pipeline.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var incoming) && IsSafe(incoming.ToString())
            ? incoming.ToString()
            : Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty(LogProperties.CorrelationId, correlationId))
        {
            await _next(context);
        }
    }

    /// <summary>Client-supplied ids end up in logs: allow only short, plain values (no log injection).</summary>
    private static bool IsSafe(string value) =>
        value.Length is > 0 and <= 64 && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');
}
