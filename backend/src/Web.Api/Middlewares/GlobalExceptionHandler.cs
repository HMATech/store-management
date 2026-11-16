using Microsoft.AspNetCore.Diagnostics;
using Shared;
using Web.Api.Common;

namespace Web.Api.Middlewares;

internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    public const string BadRequestCode = "General.BadRequest";

    private readonly ILogger<GlobalExceptionHandler> logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        this.logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext,
        Exception exception, CancellationToken cancellationToken)
    {
        // Minimal APIs throw this for a request they can't bind, such as ?page=abc, but only in Development, where
        // RouteHandlerOptions.ThrowOnBadRequest is on. It's the client's mistake, so answer with its status.
        if (exception is BadHttpRequestException badRequest)
        {
            logger.LogWarning(exception, "Rejected a request that could not be read");

            var badRequestResult = Error.Problem(BadRequestCode, badRequest.Message).ToProblemDetails();
            badRequestResult.Status = badRequest.StatusCode;
            httpContext.Response.StatusCode = badRequest.StatusCode;
            await httpContext.Response
                .WriteAsJsonAsync(badRequestResult, cancellationToken);

            return true;
        }

        logger.LogError(exception, "An unhandled exception has occurred");

        var problemResult = Error.System("", "").ToProblemDetails();
        await httpContext.Response
            .WriteAsJsonAsync(problemResult, cancellationToken);

        return true;
    }
}