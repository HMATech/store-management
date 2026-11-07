using Application.Abstractions.Messaging;

namespace Application.Stocks.StockAdjustments.Create;

public sealed record CreateStockAdjustmentCommand(Guid ProductId, int Quantity, string Reason,
    DateTime OccurrenceTime)
    : ICommand<CreateStockAdjustmentResponse>;
