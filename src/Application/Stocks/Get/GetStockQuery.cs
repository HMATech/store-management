using Application.Abstractions.Messaging;
using Application.Common;

namespace Application.Stocks.Get;

public sealed record GetStockQuery(string? Search, SortOrder SortOrder,
    int Page = 1, int PageSize = 10)
    : IQuery<PagedResponse<ProductStockResponse>>
{
    public override string ToString()
    {
        return $"{(string.IsNullOrEmpty(Search) ? "" : $"Search: {Search}, ")}" +
            $"Page: {Page}, PageSize: {PageSize}";
    }
}
