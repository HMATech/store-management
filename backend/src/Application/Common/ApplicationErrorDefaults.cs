namespace Application.Common;

public static class ApplicationErrorDefaults
{
    public const string PostgreSQLOnDeleteRestrictViolationCode = "23503";

    public const string ApplicationErrorTitle = "ApplicationError";
    public const string DBOperationErrorTitle = "DBOperationError";
    public const string OperationCancelledErrorTitle = "OperationError";
    public const string UnexpectedErrorTitle = "UnexpectedError";
}