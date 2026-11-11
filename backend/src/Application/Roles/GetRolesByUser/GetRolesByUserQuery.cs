using Application.Abstractions.Messaging;

namespace Application.Roles.GetRolesByUser;

public sealed record GetRolesByUserQuery(string User)
    : IQuery<List<UserRoleResponse>>;