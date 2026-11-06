using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Web.Api.Infrastructure;

namespace Web.Api.Tests.Unit;

public class CustomHttpResultsTests
{
    private const string Code = "Some.Code";
    private const string Description = "Some description";

    public static TheoryData<Error, Type> ErrorResults => new()
    {
        { Error.Problem(Code, Description), typeof(BadRequest<ProblemDetails>) },
        { ValidationError.From([]), typeof(BadRequest<ProblemDetails>) },
        { Error.NotFound(Code, Description), typeof(NotFound<ProblemDetails>) },
        { Error.Conflict(Code, Description), typeof(Conflict<ProblemDetails>) },
        { Error.System(Code, Description), typeof(ProblemHttpResult) },
    };

    [Fact]
    public void TypedFrom_OnSuccess_ReturnsSuccessResult()
    {
        var result = CustomHttpResults.TypedFrom(Result.Succeed(), static () => TypedResults.Ok());

        Assert.IsType<Ok>(result.Result);
    }

    [Fact]
    public void TypedFrom_WithValue_OnSuccess_PassesValue()
    {
        var result = CustomHttpResults.TypedFrom(Result.Succeed(5), static v => TypedResults.Ok(v));

        Assert.Equal(5, Assert.IsType<Ok<int>>(result.Result).Value);
    }

    [Fact]
    public void TypedFrom_WithContext_OnSuccess_PassesValueAndContext()
    {
        var context = new DefaultHttpContext();
        HttpContext? received = null;

        var result = CustomHttpResults.TypedFrom(Result.Succeed(5), (v, c) =>
        {
            received = c;
            return TypedResults.Ok(v);
        }, context);

        Assert.Same(context, received);
        Assert.IsType<Ok<int>>(result.Result);
    }

    [Fact]
    public void TypedFrom_WithNullContext_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => CustomHttpResults.TypedFrom(Result.Succeed(5),
            static (int v, HttpContext c) => TypedResults.Ok(v), null!));
    }

    [Theory]
    [MemberData(nameof(ErrorResults))]
    public void TypedFrom_OnFailure_MapsErrorToResult(Error error, Type expected)
    {
        var result = CustomHttpResults.TypedFrom(Result.Fail(error), static () => TypedResults.Ok());

        Assert.IsType(expected, result.Result);
    }

    [Theory]
    [MemberData(nameof(ErrorResults))]
    public void TypedFrom_WithValue_OnFailure_DoesNotCallSuccessCallback(Error error, Type expected)
    {
        var result = CustomHttpResults.TypedFrom(Result.Fail<int>(error), UnexpectedSuccess);

        Assert.IsType(expected, result.Result);
    }

    [Theory]
    [MemberData(nameof(ErrorResults))]
    public void TypedFrom_WithContext_OnFailure_DoesNotCallSuccessCallback(Error error, Type expected)
    {
        var result = CustomHttpResults.TypedFrom(Result.Fail<int>(error),
            static (int v, HttpContext c) => UnexpectedSuccess(v), new DefaultHttpContext());

        Assert.IsType(expected, result.Result);
    }

    [Theory]
    [MemberData(nameof(ErrorResults))]
    public void TypedProblemFrom_OnFailure_MapsErrorToResult(Error error, Type expected)
    {
        Assert.IsType(expected, CustomHttpResults.TypedProblemFrom(Result.Fail(error)).Result);
    }

    [Fact]
    public void TypedProblemFrom_OnSuccess_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => CustomHttpResults.TypedProblemFrom(Result.Succeed()));
    }

    [Fact]
    public void ProblemFrom_OnFailure_ReturnsProblemWithStatusOfError()
    {
        var problem = CustomHttpResults.ProblemFrom(Result.Fail(Error.NotFound(Code, Description)));

        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal(MediaTypeNames.Application.ProblemJson, problem.ContentType);
    }

    [Fact]
    public void ProblemFrom_OnSuccess_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => CustomHttpResults.ProblemFrom(Result.Succeed()));
    }

    [Theory]
    [InlineData(ErrorType.Problem, StatusCodes.Status400BadRequest, ResponseErrorTypeUri.BadRequest)]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound, ResponseErrorTypeUri.NotFound)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict, ResponseErrorTypeUri.Conflict)]
    public void CreateProblemDetails_ForClientError_ExposesCodeAndDescription(ErrorType type, int status, string uri)
    {
        var details = CustomHttpResults.CreateProblemDetails(new Error(Code, Description, type));

        Assert.Equal(Code, details.Title);
        Assert.Equal(Description, details.Detail);
        Assert.Equal(status, details.Status);
        Assert.Equal(uri, details.Type);
    }

    [Fact]
    public void CreateProblemDetails_ForSystemError_HidesInternalDetails()
    {
        var details = CustomHttpResults.CreateProblemDetails(Error.System("Internal.Code", "Secret connection info"));

        Assert.Equal(CustomHttpResults.SystemErrorTitle, details.Title);
        Assert.Equal(CustomHttpResults.SystemErrorDetail, details.Detail);
        Assert.Equal(StatusCodes.Status500InternalServerError, details.Status);
        Assert.Equal(ResponseErrorTypeUri.ServerError, details.Type);
    }

    [Fact]
    public void CreateProblemDetails_ForValidationError_IncludesFieldErrors()
    {
        var error = ValidationError.From(new Dictionary<string, List<Error>>
        {
            ["Name"] = [Error.Problem("NotEmpty", "Must not be empty")]
        });

        var details = CustomHttpResults.CreateProblemDetails(error);

        Assert.Equal(StatusCodes.Status400BadRequest, details.Status);
        Assert.Same(error.FieldErrors, details.Extensions[CustomHttpResults.ValidationErrorFieldName]);
    }

    private static Ok<int> UnexpectedSuccess(int value)
    {
        Assert.Fail("The success callback must not run for a failed result");
        return TypedResults.Ok(value);
    }
}
