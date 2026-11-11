using Shared;

namespace Domain.Roles;

public static class RoleErrors
{
    public static Error NotFound(int id) => Error.NotFound(
        "Roles.NotFound",
        $"Role with id '{id}' was not found."
    );

    public static Error NotFound(string name) => Error.NotFound(
        "Roles.NotFound",
        $"Role with name '{name}' was not found."
    );

    public static Error AlreadyExist(string name) => Error.Conflict(
        "Roles.AlreadyExist",
        $"Role with name '{name}' already exists"
    );

    public static Error AlreadyAssigned(int roleId, string user) => Error.Conflict(
        "Roles.AlreadyAssigned",
        $"Role with id '{roleId}' has already been assigned to user '{user}'"
    );

    public static Error NotAssigned(int roleId, string user) => Error.Problem(
        "Roles.NotAssigned",
        $"Role with id '{roleId}' hasn't already been assigned to user '{user}'"
    );
}