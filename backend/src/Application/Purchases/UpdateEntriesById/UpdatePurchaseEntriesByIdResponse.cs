namespace Application.Purchases.UpdateEntriesById;

public sealed record UpdatePurchaseEntriesByIdResponse(
    Guid Id, ProductEntryResponse[] ProductEntries,
    decimal TotalCost, DateTime LastUpdatedTime
);
