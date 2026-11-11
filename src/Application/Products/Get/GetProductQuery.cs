using Application.Abstractions.Messaging;
using Application.Common;

namespace Application.Products.Get;

/// <param name="StockStatus">Keeps only products whose stock has this status, matched ignoring case.</param>
public sealed record GetProductQuery(string? Search, SortOrder SortOrder,
    int Page = 1, int PageSize = 10, string? StockStatus = null)
    : IQuery<PagedResponse<ProductResponse>>
{
    public override string ToString()
    {
        return $"{(string.IsNullOrEmpty(Search) ? "" : $"Search: {Search}, ")}" +
            $"{(string.IsNullOrEmpty(StockStatus) ? "" : $"StockStatus: {StockStatus}, ")}" +
            $"Page: {Page}, PageSize: {PageSize}";
    }
}
