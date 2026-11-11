namespace Shared.Tests;

public class ErrorTests
{
    [Fact]
    public void None_HasNoCodeOrDescription()
    {
        Assert.Equal(string.Empty, Error.None.Code);
        Assert.Equal(string.Empty, Error.None.Description);
        Assert.Equal(ErrorType.None, Error.None.ErrorType);
    }

    [Fact]
    public void NullValue_IsSystemError()
    {
        Assert.Equal("General.Null", Error.NullValue.Code);
        Assert.Equal(ErrorType.System, Error.NullValue.ErrorType);
    }

    [Fact]
    public void Factories_SetCodeDescriptionAndType()
    {
        AssertError(Error.System("c", "d"), ErrorType.System);
        AssertError(Error.Problem("c", "d"), ErrorType.Problem);
        AssertError(Error.NotFound("c", "d"), ErrorType.NotFound);
        AssertError(Error.Conflict("c", "d"), ErrorType.Conflict);
    }

    [Fact]
    public void Errors_WithSameValues_AreEqual()
    {
        Assert.Equal(Error.Problem("c", "d"), Error.Problem("c", "d"));
        Assert.NotEqual(Error.Problem("c", "d"), Error.Conflict("c", "d"));
    }

    [Fact]
    public void ValidationError_From_KeepsFieldErrors()
    {
        var fieldError = Error.Problem("NotEmpty", "Must not be empty");

        var error = ValidationError.From(new Dictionary<string, List<Error>> { ["Name"] = [fieldError] });

        Assert.Equal("Validation.General", error.Code);
        Assert.Equal(ErrorType.Validation, error.ErrorType);
        Assert.Equal(fieldError, Assert.Single(error.FieldErrors["Name"]));
    }

    private static void AssertError(Error error, ErrorType expectedType)
    {
        Assert.Equal("c", error.Code);
        Assert.Equal("d", error.Description);
        Assert.Equal(expectedType, error.ErrorType);
    }
}
