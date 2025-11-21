using System.Net;
using System.Text.Json;
using Shared.Contracts;

namespace Server.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BusinessException bex)
        {
            _logger.LogWarning(bex, "Business exception");
            await WriteResponse(context, HttpStatusCode.BadRequest, ApiResponse.Fail<object>(bex.Message, bex.Code));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteResponse(context, HttpStatusCode.InternalServerError, ApiResponse.Error<object>("Unhandled server error", code: "UNHANDLED"));
        }
    }

    private static async Task WriteResponse(HttpContext ctx, HttpStatusCode code, object payload)
    {
        if (ctx.Response.HasStarted) return;
        ctx.Response.Clear();
        ctx.Response.StatusCode = (int)code;
        ctx.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(ctx.Response.Body, payload, payload.GetType());
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseApiExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionHandlingMiddleware>();
}
