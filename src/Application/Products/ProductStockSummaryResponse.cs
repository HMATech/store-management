using Application.Stocks;

namespace Application.Products;

/// <summary>The stock shown with a product. <c>/stocks</c> has the full breakdown.</summary>
public sealed record ProductStockSummaryResponse(long Quantity, StockStatus Status);
