using Microsoft.AspNetCore.Mvc;
using Shared;
using Web.Api.Infrastructure;

namespace Web.Api.Common;

public static class ResultExtensions
{
    public static TOut MatchFrom<TOut>(this Result result,
        Func<TOut> onSucceeded,
        Func<Result, TOut> onFailed)
    {
        return result.IsSuccess
            ? onSucceeded()
            : onFailed(result);
    }

    public static TOut MatchFrom<TIn, TOut>(this Result<TIn> result,
        Func<TIn, TOut> onSucceeded,
        Func<Result<TIn>, TOut> onFailed)
    {
        return result.IsSuccess
            ? onSucceeded(result.Value)
            : onFailed(result);
    }
}