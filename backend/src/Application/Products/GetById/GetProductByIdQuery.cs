using Application.Abstractions.Messaging;

namespace Application.Products.GetById;

public sealed record GetProductByIdQuery(Guid Id)
    : IQuery<ProductResponse>;