using Application.Abstractions.Messaging;

namespace Application.Roles.Create;

public sealed record CreateRoleCommand(string Role, string? Description) 
    : ICommand<CreateRoleResponse>;