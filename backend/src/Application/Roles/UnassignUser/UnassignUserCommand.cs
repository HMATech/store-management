using Application.Abstractions.Messaging;

namespace Application.Roles.UnassignUser;

public sealed record UnassignUserCommand(int RoleId, string User)
    : ICommand;