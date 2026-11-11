using Application.Abstractions.Messaging;
using Application.Products.ProductPrices;
using Application.Products.ProductPrices.GetById;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Products.ProductPrices;

internal sealed class GetById : ProductPriceEndpoint
{
    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        base.MapEndpoint(app).MapGet("/{id:guid}", static async (
            Guid id,
            IQueryHandler<GetProductPriceByIdQuery, ProductPriceResponse> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var query = new GetProductPriceByIdQuery(id);

            var result = await handler.HandleAsync(query, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static (r) => TypedResults.Ok(r));
        });
        return app;
    }
}