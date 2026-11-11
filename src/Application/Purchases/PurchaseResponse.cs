namespace Application.Purchases;

public sealed record PurchaseResponse(
    Guid Id, string Title, string[] Tags,
    ProductEntryResponse[] ProductEntries,
    decimal TotalCost,
    DateTime OccurrenceTime, DateTime CreatedTime,
    DateTime LastUpdatedTime
);

public sealed record ProductEntryResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal UnitCost,
    int Quantity,
    decimal TotalCost
);
