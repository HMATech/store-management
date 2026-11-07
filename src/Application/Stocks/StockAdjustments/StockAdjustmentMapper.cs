using Domain.Stocks;

namespace Application.Stocks.StockAdjustments;

public static class StockAdjustmentMapper
{
    public static StockAdjustmentResponse ToStockAdjustmentResponse(this StockAdjustment adjustment)
    {
        return new StockAdjustmentResponse(adjustment.Id, adjustment.ProductId, adjustment.Quantity,
            adjustment.Reason, adjustment.OccurrenceTime, adjustment.CreatedTime);
    }
}
