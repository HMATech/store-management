using Application.Abstractions.Messaging;
using Application.Common;

namespace Application.Products.ProductTags.Get;

public sealed record GetProductTagQuery(string? Search, SortOrder SortOrder)
    : IQuery<List<ProductTagResponse>>
{
    public override string ToString()
    {
        return $"{(string.IsNullOrEmpty(Search) ? "" : $"Search: {Search}")}" +
            $"SortOrder: {SortOrder}";
    }
}