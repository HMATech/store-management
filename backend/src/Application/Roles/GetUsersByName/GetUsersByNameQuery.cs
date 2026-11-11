using Application.Abstractions.Messaging;
using Application.Users;

namespace Application.Roles.GetUsersByName;

public sealed record GetUsersByNameQuery(string Name)
    : IQuery<List<UserResponse>>;