namespace Shared;

public class Result
{
    public bool IsSuccess { get; init; }
    public Error Error { get; init; }

    public bool IsFailed => !IsSuccess;

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None
            || !isSuccess && error == Error.None)
        {
            throw new ArgumentException("Invalid result");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public static implicit operator Result(Error error)
        => error.ErrorType == ErrorType.None ? Succeed() : Fail(error);

    public static Result Succeed() => new(true, Error.None);

    public static Result Fail(Error error) => new(false, error);

    public static Result<TValue> Succeed<TValue>(TValue value)
        => new(value, true, Error.None);

    public static Result<TValue> Fail<TValue>(Error error)
        => new(default, false, error);
}

public class Result<TValue> : Result
{
    private readonly TValue? value;

    public TValue Value => IsSuccess
        ? value!
        : throw new InvalidOperationException("Value of a failed result cannot be accessed");

    internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        this.value = value;
    }

    public static implicit operator Result<TValue>(TValue? value) =>
        value is not null ? Succeed(value) : Fail<TValue>(Error.NullValue);

    public static implicit operator Result<TValue>(Error error) =>
        Fail<TValue>(error);

    public static Result<TValue> ValidationFail(Error error)
        => new(default, false, error);
}
