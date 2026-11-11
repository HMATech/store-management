using Application.Abstractions.Messaging;
using Application.Common;

namespace Application.Products.ProductPrices.Get;

public sealed record GetProductPriceQuery(string? Search, SortOrder SortOrder,
    int Page = 1, int PageSize = 10)
    : IQuery<PagedResponse<ProductPriceResponse>>
{
    public override string ToString()
    {
        return $"{(string.IsNullOrEmpty(Search) ? "" : $"Search: {Search}")}. " +
            $"SortOrder: {SortOrder}, Page: {Page}, PageSize: {PageSize}";
    }
}