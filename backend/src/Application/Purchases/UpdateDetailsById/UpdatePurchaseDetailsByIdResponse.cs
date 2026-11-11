namespace Application.Purchases.UpdateDetailsById;

public sealed record UpdatePurchaseDetailsByIdResponse(
    Guid Id, string Title, string[] Tags,
    DateTime OccurenceTime, DateTime LastUpdatedTime
);