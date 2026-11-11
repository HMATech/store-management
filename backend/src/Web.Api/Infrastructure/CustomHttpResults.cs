using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Shared;

namespace Web.Api.Infrastructure;

public static class CustomHttpResults
{
    public const string SystemErrorTitle = "System Error";
    public const string SystemErrorDetail = "An error has occurred in our end while processing the request";
    public const string ValidationErrorFieldName = "fieldErrors";

    public static Results<
        TResult,
        BadRequest<ProblemDetails>,
        NotFound<ProblemDetails>,
        Conflict<ProblemDetails>,
        ProblemHttpResult> TypedFrom<TResult>(Result result, Func<TResult> onSuccess) where TResult : IResult
    {
        if (result.IsSuccess)
        {
            return onSuccess();
        }

        return InternalProblemFrom<TResult>(result);
    }

    public static Results<
        TResult,
        BadRequest<ProblemDetails>,
        NotFound<ProblemDetails>,
        Conflict<ProblemDetails>,
        ProblemHttpResult> TypedFrom<TValue, TResult>(Result<TValue> result,
            Func<TValue, TResult> onSuccess) where TResult : IResult
    {
        if (result.IsSuccess)
        {
            return onSuccess(result.Value);
        }

        return InternalProblemFrom<TResult>(result);
    }

    public static Results<
        TResult,
        BadRequest<ProblemDetails>,
        NotFound<ProblemDetails>,
        Conflict<ProblemDetails>,
        ProblemHttpResult> TypedFrom<TValue, TResult>(Result<TValue> result,
            Func<TValue, HttpContext, TResult> onSuccess,
            HttpContext context) where TResult : IResult
    {
        ArgumentNullException.ThrowIfNull(context);

        if (result.IsSuccess)
        {
            return onSuccess(result.Value, context);
        }

        return InternalProblemFrom<TResult>(result);
    }

    public static Results<
        BadRequest<ProblemDetails>,
        NotFound<ProblemDetails>,
        Conflict<ProblemDetails>,
        ProblemHttpResult> TypedProblemFrom(Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Cannot create custom problem http result from a successful result");
        }

        return InternalProblemFrom(result);
    }

    public static ProblemHttpResult ProblemFrom(Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Cannot create custom problem http result from a successful result");
        }

        return TypedResults.Problem(CreateProblemDetails(result.Error));
    }

    private static Results<
        TResult,
        BadRequest<ProblemDetails>,
        NotFound<ProblemDetails>,
        Conflict<ProblemDetails>,
        ProblemHttpResult> InternalProblemFrom<TResult>(Result result) where TResult : IResult
    {
        var problemDetails = CreateProblemDetails(result.Error);

        return result.Error.ErrorType switch
        {
            ErrorType.Validation or ErrorType.Problem => TypedResults.BadRequest(problemDetails),
            ErrorType.NotFound => TypedResults.NotFound(problemDetails),
            ErrorType.Conflict => TypedResults.Conflict(problemDetails),
            _ => TypedResults.Problem(problemDetails)
        };
    }

    private static Results<
        BadRequest<ProblemDetails>,
        NotFound<ProblemDetails>,
        Conflict<ProblemDetails>,
        ProblemHttpResult> InternalProblemFrom(Result result)
    {
        var problemDetails = CreateProblemDetails(result.Error);

        return result.Error.ErrorType switch
        {
            ErrorType.Validation or ErrorType.Problem => TypedResults.BadRequest(problemDetails),
            ErrorType.NotFound => TypedResults.NotFound(problemDetails),
            ErrorType.Conflict => TypedResults.Conflict(problemDetails),
            _ => TypedResults.Problem(problemDetails)
        };
    }
    
    public static ProblemDetails CreateProblemDetails(Error error)
    {
        var problemDetails = new ProblemDetails()
        {
            Title = GetTitle(error),
            Detail = GetDetail(error),
            Status = GetStatus(error),
            Type = GetProblemType(error),
            Extensions = GetErrors(error)!,
        };

        return problemDetails;
    }

    private static string GetTitle(Error error) => error.ErrorType == ErrorType.System
        ? SystemErrorTitle
        : error.Code;

    private static string GetDetail(Error error) => error.ErrorType == ErrorType.System
        ? SystemErrorDetail
        : error.Description;

    private static int GetStatus(Error error)
        => error.ErrorType switch
        {
            ErrorType.Validation or ErrorType.Problem => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

    private static string GetProblemType(Error error)
        => ResponseErrorTypeUri.Get(error.ErrorType);

    private static Dictionary<string, object?>? GetErrors(Result result)
    {
        if (result.Error is not ValidationError validationError)
        {
            return null;
        }

        return new Dictionary<string, object?>()
        {
            { ValidationErrorFieldName, validationError.FieldErrors }
        };
    }
}