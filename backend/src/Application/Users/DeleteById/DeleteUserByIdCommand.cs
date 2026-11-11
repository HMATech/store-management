using Application.Abstractions.Messaging;

namespace Application.Users.DeleteById;

public sealed record DeleteUserByIdCommand(Guid UserId)
    : ICommand;