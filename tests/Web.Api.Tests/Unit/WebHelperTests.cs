using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Shared;
using Web.Api.Common;
using Web.Api.Infrastructure;
using Web.Api.Middlewares;

namespace Web.Api.Tests.Unit;

public class WebHelperTests
{
    [Theory]
    [InlineData(ErrorType.Problem, ResponseErrorTypeUri.BadRequest)]
    [InlineData(ErrorType.Validation, ResponseErrorTypeUri.BadRequest)]
    [InlineData(ErrorType.NotFound, ResponseErrorTypeUri.NotFound)]
    [InlineData(ErrorType.Conflict, ResponseErrorTypeUri.Conflict)]
    [InlineData(ErrorType.System, ResponseErrorTypeUri.ServerError)]
    public void ResponseErrorTypeUri_MapsEveryErrorType(ErrorType type, string expected)
    {
        Assert.Equal(expected, ResponseErrorTypeUri.Get(type));
    }

    [Fact]
    public void ResponseErrorTypeUri_ForNone_Throws()
    {
        Assert.Throws<KeyNotFoundException>(() => ResponseErrorTypeUri.Get(ErrorType.None));
    }

    [Fact]
    public void ToProblemDetails_MatchesCustomHttpResults()
    {
        var error = Error.Conflict("Code", "Description");

        var details = error.ToProblemDetails();

        Assert.Equal("Code", details.Title);
        Assert.Equal(StatusCodes.Status409Conflict, details.Status);
    }

    [Fact]
    public void ToUriFullAbsolutePath_BuildsUrlFromRequest()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("example.com", 8443);
        context.Request.Path = "/brands";
        context.Request.QueryString = new QueryString("?page=2");

        var uri = context.ToUriFullAbsolutePath();

        Assert.Equal("https://example.com:8443/brands", uri.ToString());
    }

    [Fact]
    public void ToUriFullAbsolutePath_WithNullContext_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => HttpContextExtensions.ToUriFullAbsolutePath(null!));
    }

    [Fact]
    public void ToUriFullAbsolutePath_WithoutHost_Throws()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";

        Assert.Throws<UriFormatException>(() => context.ToUriFullAbsolutePath());
    }

    [Fact]
    public void MatchFrom_OnSuccess_CallsSuccessBranch()
    {
        Assert.Equal("ok", Result.Succeed().MatchFrom(() => "ok", _ => "failed"));
        Assert.Equal("5", Result.Succeed(5).MatchFrom(v => v.ToString(), _ => "failed"));
    }

    [Fact]
    public void MatchFrom_OnFailure_CallsFailureBranch()
    {
        var error = Error.Problem("Code", "Description");

        Assert.Equal("Code", Result.Fail(error).MatchFrom(() => "ok", r => r.Error.Code));
        Assert.Equal("Code", Result.Fail<int>(error).MatchFrom(_ => "ok", r => r.Error.Code));
    }

    [Fact]
    public async Task GlobalExceptionHandler_WritesSystemProblem_WithoutLeakingException()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(context, new InvalidOperationException("secret detail"), default);

        Assert.True(handled);
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        Assert.DoesNotContain("secret detail", body);

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var problem = JsonSerializer.Deserialize<ProblemDetails>(body, options)!;
        Assert.Equal(CustomHttpResults.SystemErrorTitle, problem.Title);
        Assert.Equal(StatusCodes.Status500InternalServerError, problem.Status);
    }
}
