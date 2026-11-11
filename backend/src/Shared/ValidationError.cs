namespace Shared;

public sealed record ValidationError : Error
{
    public IReadOnlyDictionary<string, IReadOnlyList<Error>> FieldErrors { get; init; }

    private ValidationError(IReadOnlyDictionary<string, IReadOnlyList<Error>> errors)
        : base(
            "Validation.General",
            "One or more validation errors occurred",
            ErrorType.Validation
        )
    {
        FieldErrors = errors;
    }

    public static ValidationError From(Dictionary<string, List<Error>> errors)
        => FromReadOnly(errors.ToDictionary(kv => kv.Key, kv => kv.Value as IReadOnlyList<Error>));
    
    public static ValidationError FromReadOnly(IReadOnlyDictionary<string, IReadOnlyList<Error>> errors)
        => new(errors);
}