using Shared;

namespace Domain.Brands;

public static class BrandErrors
{
    public static Error AlreadyExists(string name) => Error.Conflict(
        "Brands.AlreadyExist",
        $"Brand with name '{name}' already exists"
    );

    public static Error InUse(int id) => Error.Conflict(
        "Brands.InUse",
        $"Brand with id '{id}' is still in use"
    );

    public static Error NotFound(int id) => Error.NotFound(
        "Brands.NotFound",
        $"Brand with id '{id}' was not found"
    );

    public static Error NotFound(string name) => Error.NotFound(
        "Brands.NotFound",
        $"Brand with name '{name}' was not found"
    );
}