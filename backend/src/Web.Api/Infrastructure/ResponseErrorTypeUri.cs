using Shared;

namespace Web.Api.Infrastructure;

public static class ResponseErrorTypeUri
{
    public const string BadRequest = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
    public const string NotFound = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
    public const string Conflict = "https://tools.ietf.org/html/rfc7231#section-6.5.8";
    public const string ServerError = "https://tools.ietf.org/html/rfc7231#section-6.6.1";

    private readonly static Dictionary<ErrorType, string> ErrorPairs = new()
    {
        { ErrorType.Problem, BadRequest },
        { ErrorType.Validation, BadRequest },
        { ErrorType.NotFound, NotFound },
        { ErrorType.Conflict, Conflict },
        { ErrorType.System, ServerError },
    };

    public static string Get(ErrorType errorType)
    {
        return ErrorPairs[errorType];
    }
}