using Shared;

namespace Domain.Users;

public static class UserErrors
{
    public static Error NotFound(Guid userId) => Error.NotFound(
        "Users.NotFound",
        $"User with id '{userId}' was not found"
    );

    public static Error NotFound(string email) => Error.NotFound(
        "Users.NotFound",
        $"User with email '{email}' was not found"
    );

    public static Error EmailAlreadyRegistered(string email) => Error.Conflict(
        "Users.EmailAlreadyRegistered",
        $"User with email '{email}' is already registered"
    );

    public static Error IncorrectCredential() => Error.Problem(
        "Users.IncorrectCredential",
        $"Incorrect email or password"
    );
}