using Microsoft.AspNetCore.Diagnostics;
using Shared;
using Web.Api.Common;

namespace Web.Api.Middlewares;

internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        this.logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext,
        Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "An unhandled exception has occurred");

        var problemResult = Error.System("", "").ToProblemDetails();
        await httpContext.Response
            .WriteAsJsonAsync(problemResult, cancellationToken);

        return true;
    }
}