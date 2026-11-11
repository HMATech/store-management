using Application.Abstractions.Messaging;
using Application.Common;
using Application.Products;
using Application.Products.Get;
using Web.Api.Infrastructure;
namespace Web.Api.Endpoints.Products;

internal sealed class Get : ProductEndpoint
{
    public sealed record GetProductRequest(string? Search,
        string? SortOrder, int Page = 1, int PageSize = 10, string? StockStatus = null);

    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/", static async (
            [AsParameters] GetProductRequest request,
            IQueryHandler<GetProductQuery, PagedResponse<ProductResponse>> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var query = new GetProductQuery(request.Search,
                Enum.TryParse<SortOrder>(request.SortOrder, out var order)
                ? order : SortOrder.ASC, request.Page, request.PageSize, request.StockStatus);

            var result = await handler.HandleAsync(query, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static (r) => TypedResults.Ok(r));
        });
        return app;
    }
}