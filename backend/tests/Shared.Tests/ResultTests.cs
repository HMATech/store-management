namespace Shared.Tests;

public class ResultTests
{
    private static readonly Error SomeError = Error.Problem("Some.Code", "Some description");

    [Fact]
    public void Succeed_ReturnsSuccessWithNoError()
    {
        var result = Result.Succeed();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailed);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Fail_ReturnsFailureWithError()
    {
        var result = Result.Fail(SomeError);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailed);
        Assert.Equal(SomeError, result.Error);
    }

    [Fact]
    public void Fail_WithNoneError_Throws()
    {
        Assert.Throws<ArgumentException>(() => Result.Fail(Error.None));
    }

    [Fact]
    public void ImplicitConversion_FromError_ReturnsFailure()
    {
        Result result = SomeError;

        Assert.True(result.IsFailed);
        Assert.Equal(SomeError, result.Error);
    }

    [Fact]
    public void ImplicitConversion_FromNoneError_ReturnsSuccess()
    {
        Result result = Error.None;

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void GenericSucceed_ExposesValue()
    {
        var result = Result.Succeed(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GenericFail_AccessingValue_Throws()
    {
        var result = Result.Fail<int>(SomeError);

        Assert.True(result.IsFailed);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void GenericImplicitConversion_FromValue_ReturnsSuccess()
    {
        Result<string> result = "value";

        Assert.True(result.IsSuccess);
        Assert.Equal("value", result.Value);
    }

    [Fact]
    public void GenericImplicitConversion_FromNull_ReturnsNullValueError()
    {
        Result<string> result = (string?)null;

        Assert.True(result.IsFailed);
        Assert.Equal(Error.NullValue, result.Error);
    }

    [Fact]
    public void GenericImplicitConversion_FromError_ReturnsFailure()
    {
        Result<string> result = SomeError;

        Assert.True(result.IsFailed);
        Assert.Equal(SomeError, result.Error);
    }

    [Fact]
    public void ValidationFail_ReturnsFailureWithError()
    {
        var result = Result<int>.ValidationFail(SomeError);

        Assert.True(result.IsFailed);
        Assert.Equal(SomeError, result.Error);
    }
}
