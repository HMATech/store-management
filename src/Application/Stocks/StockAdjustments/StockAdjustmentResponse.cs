namespace Application.Stocks.StockAdjustments;

public sealed record StockAdjustmentResponse(Guid Id, Guid ProductId, int Quantity, string Reason,
    DateTime OccurrenceTime, DateTime CreatedTime);
