using Application.Abstractions.Messaging;

namespace Application.Stocks.GetByProductId;

public sealed record GetStockByProductIdQuery(Guid ProductId)
    : IQuery<ProductStockResponse>;
