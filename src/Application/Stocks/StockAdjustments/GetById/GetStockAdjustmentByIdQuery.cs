using Application.Abstractions.Messaging;

namespace Application.Stocks.StockAdjustments.GetById;

public sealed record GetStockAdjustmentByIdQuery(Guid Id)
    : IQuery<StockAdjustmentResponse>;
