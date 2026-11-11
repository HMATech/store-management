using Application.Abstractions.Messaging;
using Application.Common;

namespace Application.Users.Get;

public sealed record GetUserQuery(string? Search, int Page = 1, int PageSize = 10)
    : IQuery<PagedResponse<UserResponse>>
{
    public override string ToString()
    {
        return $"{(string.IsNullOrEmpty(Search) ? "" : $"Search: {Search}")}" +
            $"Page: {Page}, PageSize: {PageSize}";
    }
}