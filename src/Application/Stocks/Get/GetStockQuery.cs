using Application.Abstractions.Messaging;
using Application.Common;

namespace Application.Stocks.Get;

/// <param name="Status">Keeps only products with this <see cref="StockStatus"/>, matched ignoring case.</param>
public sealed record GetStockQuery(string? Search, SortOrder SortOrder,
    int Page = 1, int PageSize = 10, string? Status = null)
    : IQuery<PagedResponse<ProductStockResponse>>
{
    public override string ToString()
    {
        return $"{(string.IsNullOrEmpty(Search) ? "" : $"Search: {Search}, ")}" +
            $"{(string.IsNullOrEmpty(Status) ? "" : $"Status: {Status}, ")}" +
            $"Page: {Page}, PageSize: {PageSize}";
    }
}
