using Application.Abstractions.Messaging;

namespace Application.Stocks.StockAdjustments.DeleteById;

public sealed record DeleteStockAdjustmentByIdCommand(Guid Id)
    : ICommand;
