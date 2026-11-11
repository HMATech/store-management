using Application.Abstractions.Messaging;
using Application.Products;
using Application.Products.GetById;
using Web.Api.Infrastructure;
namespace Web.Api.Endpoints.Products;

internal sealed class GetById : ProductEndpoint
{
    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/{id:guid}", static async (
            Guid id,
            IQueryHandler<GetProductByIdQuery, ProductResponse> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var query = new GetProductByIdQuery(id);

            var result = await handler.HandleAsync(query, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static (r) => TypedResults.Ok(r));
        });
        return app;
    }
}