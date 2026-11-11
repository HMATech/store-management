using Application.Abstractions.Messaging;

namespace Application.Roles.UpdateById;

public sealed record UpdateRoleByIdCommand(int Id, string Name,
    string? Description) 
    : ICommand<UpdateRoleByIdResponse>;