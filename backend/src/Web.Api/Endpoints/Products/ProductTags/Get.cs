using Application.Abstractions.Messaging;
using Application.Common;
using Application.Products.ProductTags;
using Application.Products.ProductTags.Get;
using Web.Api.Infrastructure;
namespace Web.Api.Endpoints.Products.ProductTags;

internal sealed class Get : ProductTagEndpoint
{
    public sealed record GetProductTagRequest(string? Search, string? SortOrder);

    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        base.MapEndpoint(app).MapGet("/", static async (
            [AsParameters] GetProductTagRequest request,
            IQueryHandler<GetProductTagQuery, List<ProductTagResponse>> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var query = new GetProductTagQuery(request.Search,
                Enum.TryParse<SortOrder>(request.SortOrder, out var order)
                ? order : SortOrder.ASC);

            var result = await handler.HandleAsync(query, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static (r) => TypedResults.Ok(r));
        });
        return app;
    }
}