using System.Diagnostics;

using Serilog.Context;

namespace WLVSSeatSync.Api.Middleware;

internal sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreate(context.Request.Headers[HeaderName]);

        context.TraceIdentifier = correlationId;
        Activity.Current?.SetTag("correlation.id", correlationId); // shows up in traces too
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    // Headers are user input: accept only short, safe values, otherwise make a new ID.
    private static string GetOrCreate(string? incoming) =>
        !string.IsNullOrEmpty(incoming)
        && incoming.Length <= 64
        && incoming.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
            ? incoming
            : Guid.NewGuid().ToString("N");
}
