using Application.Abstractions.Messaging;
using Application.Common;
using Application.Products.ProductPrices;
using Application.Products.ProductPrices.Get;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Products.ProductPrices;

internal sealed class Get : ProductPriceEndpoint
{
    public sealed record GetProductPriceRequest(string? Search, string? SortOrder,
        int Page = 1, int PageSize = 10);

    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        base.MapEndpoint(app).MapGet("/", static async (
            [AsParameters] GetProductPriceRequest request,
            IQueryHandler<GetProductPriceQuery, PagedResponse<ProductPriceResponse>> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var query = new GetProductPriceQuery(request.Search,
                Enum.TryParse<SortOrder>(request.SortOrder, out var order)
                ? order : SortOrder.ASC, request.Page, request.PageSize);

            var result = await handler.HandleAsync(query, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static (r) => TypedResults.Ok(r));
        });
        return app;
    }
}