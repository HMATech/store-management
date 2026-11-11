using Application.Abstractions.Messaging;

namespace Application.Sales.GetById;

public sealed record GetSaleByIdQuery(Guid Id)
    : IQuery<SaleResponse>;