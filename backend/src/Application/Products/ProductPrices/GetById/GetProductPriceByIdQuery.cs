using Application.Abstractions.Messaging;

namespace Application.Products.ProductPrices.GetById;

public sealed record GetProductPriceByIdQuery(Guid Id)
    : IQuery<ProductPriceResponse>;