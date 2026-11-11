using Application.Abstractions.Messaging;

namespace Application.Roles.AssignUser;

public sealed record AssignUserCommand(int RoleId, string User)
    : ICommand<AssignUserResponse>;