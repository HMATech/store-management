using Application.Abstractions.Messaging;
using Application.Common;

namespace Application.Stocks.StockAdjustments.Get;

/// <summary>Lists adjustments, newest first, optionally only those of one product.</summary>
public sealed record GetStockAdjustmentQuery(Guid? ProductId, int Page = 1, int PageSize = 10)
    : IQuery<PagedResponse<StockAdjustmentResponse>>
{
    public override string ToString()
    {
        return $"{(ProductId == null ? "" : $"ProductId: {ProductId}, ")}Page: {Page}, PageSize: {PageSize}";
    }
}
