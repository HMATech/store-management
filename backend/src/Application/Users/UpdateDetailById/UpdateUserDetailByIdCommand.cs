using Application.Abstractions.Messaging;

namespace Application.Users.UpdateDetailById;

public sealed record UpdateUserDetailByIdCommand(Guid UserId, string FirstName, string? LastName)
    : ICommand<UpdateUserDetailByIdResponse>;