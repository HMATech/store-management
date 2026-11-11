using Shared;

namespace Domain.Purchases;

public static class PurchaseErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "Purchase.NotFound",
        $"Purchase with id {id} was not found."
    );

    public static Error ContainNonExistingProducts(IEnumerable<Guid> ids) => Error.NotFound(
        "Purchase.NonExistingProduct(s)",
        $"One or more products with id(s) '{string.Join(", ", ids)}' " +
            "were not found."
    );

    public static Error ContainForeignEntries(Guid purchaseId, IEnumerable<Guid> entryIds) => Error.NotFound(
        "Purchase.NonExistingEntry(s)",
        $"One or more product entry with id(s) '{string.Join(", ", entryIds)}' " +
        $"are not part of purchase with id '{purchaseId}'. Set the entry id to null if you intend to add new entry(s)."
    );
}