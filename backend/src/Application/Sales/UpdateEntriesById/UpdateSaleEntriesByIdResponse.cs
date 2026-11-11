namespace Application.Sales.UpdateEntriesById;

public sealed record UpdateSaleEntriesByIdResponse(
    Guid Id, ProductEntryResponse[] ProductEntries,
    decimal TotalPrice, DateTime LastUpdatedTime
);