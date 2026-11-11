namespace Application.Products.ProductPrices;

public sealed record ProductPriceResponse(
    Guid Id,
    decimal Value,
    Guid ProductId, DateTime CreatedTime,
    DateTime ValidFromTime, DateTime? ValidToTime);