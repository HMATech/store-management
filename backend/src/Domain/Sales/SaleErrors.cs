using Shared;

namespace Domain.Sales;

public static class SaleErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "Sale.NotFound",
        $"Sale with id {id} was not found."
    );

    public static Error ContainNonExistingProducts(IEnumerable<Guid> ids) => Error.NotFound(
        "Sale.NonExistingProduct(s)",
        $"One or more products with id(s) '{string.Join(", ", ids)}' " +
            "were not found."
    );

    public static Error ContainForeignEntries(Guid SaleId, IEnumerable<Guid> entryIds) => Error.NotFound(
        "Sale.NonExistingEntry(s)",
        $"One or more product entry with id(s) '{string.Join(", ", entryIds)}' " +
        $"are not part of Sale with id '{SaleId}'. Set the entry id to null if you intend to add new entry(s)."
    );
}