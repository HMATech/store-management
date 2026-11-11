using Application.Abstractions.Messaging;
using Application.Products.ProductUnits;
using Application.Products.ProductUnits.GetById;
using Web.Api.Infrastructure;
namespace Web.Api.Endpoints.Products.ProductUnits;

internal sealed class GetById : ProductUnitEndpoint
{
    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        base.MapEndpoint(app).MapGet("/{id:int}", static async (
            int id,
            IQueryHandler<GetProductUnitByIdQuery, ProductUnitResponse> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var query = new GetProductUnitByIdQuery(id);

            var result = await handler.HandleAsync(query, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static (r) => TypedResults.Ok(r));
        });
        return app;
    }
}