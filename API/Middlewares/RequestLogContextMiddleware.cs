using API.Logging;
using Serilog.Context;

namespace API.Middlewares;

/// <summary>
/// Adds the tenant and user of the request to every log event written while it is processed, so any
/// service log line can be filtered by school or user in Datadog. Runs after authentication and
/// TenantResolutionMiddleware, which establish both.
/// </summary>
public sealed class RequestLogContextMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLogContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var pushed = new List<IDisposable>();
        try
        {
            foreach (var (name, value) in RequestLogContext.Collect(context))
                pushed.Add(LogContext.PushProperty(name, value));

            await _next(context);
        }
        finally
        {
            for (var i = pushed.Count - 1; i >= 0; i--)
                pushed[i].Dispose();
        }
    }
}
