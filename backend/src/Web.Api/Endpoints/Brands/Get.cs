using Application.Abstractions.Messaging;
using Application.Brands;
using Application.Brands.Get;
using Application.Common;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Brands;

internal sealed class Get : BrandEndpoint
{
    public sealed record GetBrandRequest(string? Search,
        string? SortOrder, int Page = 1, int PageSize = 10);

    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/", static async ([AsParameters] GetBrandRequest request,
            IQueryHandler<GetBrandQuery, PagedResponse<BrandResponse>> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var query = new GetBrandQuery(request.Search,
                Enum.TryParse<SortOrder>(request.SortOrder, out var order)
                ? order : SortOrder.ASC, request.Page, request.PageSize);
            var result = await handler.HandleAsync(query, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static (r) => TypedResults.Ok(r));
        });

        return app;
    }
}