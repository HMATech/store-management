using Application.Abstractions.Messaging;
using Application.Products.ProductTags;
using Application.Products.ProductTags.GetById;
using Web.Api.Infrastructure;
namespace Web.Api.Endpoints.Products.ProductTags;

internal sealed class GetById : ProductTagEndpoint
{
    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        base.MapEndpoint(app).MapGet("/{id:int}", static async (
            int id,
            IQueryHandler<GetProductTagByIdQuery, ProductTagResponse> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var query = new GetProductTagByIdQuery(id);

            var result = await handler.HandleAsync(query, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static (r) => TypedResults.Ok(r));
        });
        return app;
    }
}