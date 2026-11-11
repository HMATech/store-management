namespace Application.Products.UpdatePriceById;

public sealed record UpdateProductPriceByIdResponse(
    Guid Id, Guid ProductId, Guid PreviousPriceId,
    decimal Value, DateTime ValidFrom
);