using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Shared.Contracts;

namespace Server.Filters;

public class ApiExceptionFilter : IAsyncExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;
    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger) => _logger = logger;

    public Task OnExceptionAsync(ExceptionContext context)
    {
        // Deprecated: handled by ExceptionHandlingMiddleware
        _logger.LogError(context.Exception, "Unhandled exception (filter - should be unused)." );
        var resp = ApiResponse.Error<object>("Unhandled server error", code: "UNHANDLED");
        context.Result = new ObjectResult(resp) { StatusCode = StatusCodes.Status500InternalServerError };
        context.ExceptionHandled = true;
        return Task.CompletedTask;
    }
}
