namespace Shared;

public record Error
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);
    public static readonly Error NullValue = new(
        "General.Null",
        "Null value was provided",
        ErrorType.System
    );

    public string Code { get; init; }
    public string Description { get; init; }
    public ErrorType ErrorType { get; init; }

    public Error(string code, string description, ErrorType errorType)
        => (Code, Description, ErrorType) = (code, description, errorType);

    public static Error System(string code, string description)
        => new(code, description, ErrorType.System);

    public static Error Problem(string code, string description)
        => new(code, description, ErrorType.Problem);

    public static Error NotFound(string code, string description)
        => new(code, description, ErrorType.NotFound);
    
    public static Error Conflict(string code, string description)
        => new(code, description, ErrorType.Conflict);
}