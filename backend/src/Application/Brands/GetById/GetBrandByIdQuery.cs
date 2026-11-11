using Application.Abstractions.Messaging;

namespace Application.Brands.GetById;

public sealed record GetBrandByIdQuery(int Id)
    : IQuery<BrandResponse>;