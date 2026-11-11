using Shared;

namespace Domain.Products;

public static class ProductErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "Products.NotFound",
        $"Product with id '{id}' was not found."
    );

    public static Error AlreadyExists(string name, string brand, int brandId) => Error.Conflict(
        "Products.AlreadyExist",
        $"Product with name '{name}' and brand '{brand} (id: {brandId})' already exists."
    );

    public static Error InUse(Guid id) => Error.Conflict(
        "Products.InUse",
        $"Product with id '{id}' is still in use."
    );

    public static Error PriceNotFound(Guid id) => Error.NotFound(
        "Products.PriceNotFound",
        $"Product price with id '{id}' was not found."
    );

    public static Error PriceInUse(Guid id) => Error.Conflict(
        "Products.PriceInUse",
        $"Product price with id '{id}' is still in use."
    );

    public static Error UnitNotFound(int id) => Error.NotFound(
        "Products.UnitNotFound",
        $"Product unit with id '{id}' was not found."
    );

    public static Error UnitNotFound(string name) => Error.NotFound(
        "Products.UnitNotFound",
        $"Product unit with name '{name}' was not found."
    );

    public static Error UnitAlreadyExists(string name) => Error.Conflict(
        "Products.UnitAlreadyExists",
        $"Product unit with name '{name}' already exists."
    );

    public static Error UnitIsStillInUse(int id) => Error.Conflict(
        "Products.UnitIsStillReferenced",
        $"Cannot delete product unit with id '{id}', it is still in use."
    );

    public static Error TagNotFound(int id) => Error.NotFound(
        "Products.TagNotFound",
        $"Product tag with id '{id}' was not found."
    );

    public static Error TagAlreadyExists(string name) => Error.Conflict(
        "Products.TagAlreadyExists",
        $"Product tag with name '{name}' already exists."
    );
}