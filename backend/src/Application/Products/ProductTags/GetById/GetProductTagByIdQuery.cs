using Application.Abstractions.Messaging;

namespace Application.Products.ProductTags.GetById;

public sealed record GetProductTagByIdQuery(int Id)
    : IQuery<ProductTagResponse>;