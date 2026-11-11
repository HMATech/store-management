using Application.Abstractions.Messaging;

namespace Application.Roles.GetById;

public sealed record GetRoleByIdQuery(int Id)
    : IQuery<RoleResponse>;