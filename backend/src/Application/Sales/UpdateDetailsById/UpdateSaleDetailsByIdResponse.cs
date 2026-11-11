namespace Application.Sales.UpdateDetailsById;

public sealed record UpdateSaleDetailsByIdResponse(
    Guid Id, string Title, string[] Tags,
    DateTime OccurenceTime, DateTime LastUpdatedTime
);