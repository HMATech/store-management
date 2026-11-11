using Application.Abstractions.Messaging;
using Application.Common;

namespace Application.Brands.Get;

public sealed record GetBrandQuery(string? Search, SortOrder SortOrder,
    int Page = 1, int PageSize = 10)
    : IQuery<PagedResponse<BrandResponse>>;