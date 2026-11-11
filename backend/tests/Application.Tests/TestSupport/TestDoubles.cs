using Application.Abstractions.Authentication;
using Domain.Users;
using Shared;

namespace Application.Tests.TestSupport;

internal sealed class FixedClock : IDateTimeProvider
{
    public static readonly DateTime Now = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    public DateTime UtcNow => Now;
}

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public Result VerifyResult { get; set; } = Result.Succeed();

    public string Hash(string password)
    {
        return $"hashed:{password}";
    }

    public Result Verify(string inputPassword, string hashedPassword)
    {
        return VerifyResult;
    }
}

internal sealed class FakeTokenProvider : ITokenProvider
{
    public string CreateFor(User user)
    {
        return $"token-for-{user.Id}";
    }
}

internal static class ResultAssert
{
    public static void Succeeded(Result result)
    {
        Assert.True(result.IsSuccess, $"{result.Error.Code}: {result.Error.Description}");
    }

    public static T Succeeded<T>(Result<T> result)
    {
        Assert.True(result.IsSuccess, $"{result.Error.Code}: {result.Error.Description}");
        return result.Value;
    }

    public static void Failed(Result result, string expectedCode, ErrorType expectedType)
    {
        Assert.True(result.IsFailed, "Expected a failed result");
        Assert.Equal(expectedCode, result.Error.Code);
        Assert.Equal(expectedType, result.Error.ErrorType);
    }
}
