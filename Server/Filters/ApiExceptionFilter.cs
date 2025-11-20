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
        _logger.LogError(context.Exception, "Unhandled exception.");

        var resp = ApiResponse.Fail<object>("UNHANDLED", new[] { context.Exception.Message });
        context.Result = new ObjectResult(resp) { StatusCode = StatusCodes.Status500InternalServerError };
        context.ExceptionHandled = true;
        return Task.CompletedTask;
    }
}
