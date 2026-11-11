using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Brands.Get;

internal sealed class GetBrandQueryHandler(
    IApplicationDbContext dbContext,
    ILogger<GetBrandQueryHandler> logger
) : IQueryHandler<GetBrandQuery, PagedResponse<BrandResponse>>
{
    public async Task<Result<PagedResponse<BrandResponse>>> HandleAsync(GetBrandQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var brands = dbContext.Brands
                .Where(r => string.IsNullOrEmpty(query.Search) || r.Name.Contains(query.Search))
                .AsQueryable();

            brands = query.SortOrder == SortOrder.ASC
                ? brands.OrderBy(r => r.Name)
                : brands.OrderByDescending(r => r.Name);

            var responses = brands
                .Select(brand => new BrandResponse(brand.Id, brand.Name, brand.CreatedTime));

            return await PagedResponse<BrandResponse>.CreateAsync(responses, query.Page, query.PageSize);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(GetBrandQueryHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while querying brands with '{query}'",
                query);
            return ApplicationErrors.UnexpectedError(nameof(GetBrandQueryHandler),
                $"Unexpected error has occurred while querying brands with '{query}'");
        }
    }
}