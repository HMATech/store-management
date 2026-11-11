using Application.Abstractions.Messaging;

namespace Application.Purchases.GetById;

public sealed record GetPurchaseByIdQuery(Guid Id)
    : IQuery<PurchaseResponse>;