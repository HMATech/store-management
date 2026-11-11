using Application.Abstractions.Messaging;

namespace Application.Roles.DeleteById;

public sealed record DeleteRoleByIdCommand(int Id) 
    : ICommand;