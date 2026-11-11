using Shared;
using static Application.Common.ApplicationErrorDefaults;

namespace Application.Common;

public static class ApplicationErrors
{
    public static Error ApplicationError(string layerPart, Error error) => Error.System(
        $"{layerPart}.{ApplicationErrorTitle}:{error.Code}",
        $"Application error has occurred: {error.Description}"
    );

    public static Error ApplicationError(string layerPart, string message) => Error.System(
        $"{layerPart}.{ApplicationErrorTitle}",
        message
    );

    public static Error DBOperationError(string layerPart, string message) => Error.System(
        $"{layerPart}.{DBOperationErrorTitle}",
        $"{message}"
    );

    public static Error OperationCancelledError(string layerPart, string message) => Error.System(
        $"{layerPart}.{OperationCancelledErrorTitle}",
        $"{message}"
    );

    public static Error UnexpectedError(string layerPart, string message) => Error.System(
        $"{layerPart}.{UnexpectedErrorTitle}",
        $"{message}"
    );
}