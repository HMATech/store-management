using Application.Abstractions.Messaging;

namespace Application.Products.ProductUnits.GetById;

public sealed record GetProductUnitByIdQuery(int Id)
    : IQuery<ProductUnitResponse>;