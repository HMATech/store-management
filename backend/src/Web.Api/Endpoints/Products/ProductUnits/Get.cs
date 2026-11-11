using Application.Abstractions.Messaging;
using Application.Common;
using Application.Products.ProductUnits;
using Application.Products.ProductUnits.Get;
using Web.Api.Infrastructure;
namespace Web.Api.Endpoints.Products.ProductUnits;

internal sealed class Get : ProductUnitEndpoint
{
    public sealed record GetProductUnitRequest(string? Search, string? SortOrder);

    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        base.MapEndpoint(app).MapGet("/", static async (
            [AsParameters] GetProductUnitRequest request,
            IQueryHandler<GetProductUnitQuery, List<ProductUnitResponse>> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var query = new GetProductUnitQuery(request.Search,
                Enum.TryParse<SortOrder>(request.SortOrder, out var order)
                ? order : SortOrder.ASC);

            var result = await handler.HandleAsync(query, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static (r) => TypedResults.Ok(r));
        });
        return app;
    }
}