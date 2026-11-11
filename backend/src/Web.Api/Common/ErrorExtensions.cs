using Microsoft.AspNetCore.Mvc;
using Shared;
using Web.Api.Infrastructure;

namespace Web.Api.Common;

public static class ErrorExtensions
{
    public static ProblemDetails ToProblemDetails(this Error error)
    {
        return CustomHttpResults.CreateProblemDetails(error);
    }
}