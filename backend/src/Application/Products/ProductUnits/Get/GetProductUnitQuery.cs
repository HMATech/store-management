using Application.Abstractions.Messaging;
using Application.Common;

namespace Application.Products.ProductUnits.Get;

public sealed record GetProductUnitQuery(string? Search, SortOrder SortOrder)
    : IQuery<List<ProductUnitResponse>>
{
    public override string ToString()
    {
        return $"{(string.IsNullOrEmpty(Search) ? "" : $"Search: {Search}")}" +
            $"SortOrder: {SortOrder}";
    }
}