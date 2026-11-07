namespace Application.Stocks;

/// <summary>A product's stock, worked out from everything purchased, sold and adjusted.</summary>
public sealed record ProductStockResponse(Guid ProductId, string ProductName, long Purchased, long Sold, long Adjusted)
{
    public long Quantity => Purchased - Sold + Adjusted;
}
