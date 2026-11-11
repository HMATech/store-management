using Application.Abstractions.Messaging;
using Application.Common;

namespace Application.Roles.Get;

public sealed record GetRoleQuery(string? Search,
    SortOrder SortOrder = SortOrder.ASC) : IQuery<List<RoleResponse>>
{
    public override string ToString()
    {
        return $"{(string.IsNullOrEmpty(Search) ? "" : $"Search: {Search}")}" +
            $"SortOrder: {SortOrder}";
    }
}