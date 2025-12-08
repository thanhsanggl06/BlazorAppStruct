using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Server.Middlewares;

/// <summary>
/// Middleware to add correlation ID to all requests and enrich logs
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get or generate correlation ID
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
                           ?? Guid.NewGuid().ToString();

        // Add to response headers
        context.Response.Headers.TryAdd(CorrelationIdHeader, correlationId);

        // Enrich logs with correlation ID and request info
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("RequestPath", context.Request.Path))
        using (LogContext.PushProperty("RequestMethod", context.Request.Method))
        {
            await _next(context);
        }
    }
}

/// <summary>
/// Extension method to register CorrelationIdMiddleware
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}
