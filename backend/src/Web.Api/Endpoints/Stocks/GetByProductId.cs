using Application.Abstractions.Messaging;
using Application.Stocks;
using Application.Stocks.GetByProductId;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Stocks;

internal sealed class GetByProductId : StockEndpoint
{
    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/{productId:guid}", static async (
            Guid productId,
            IQueryHandler<GetStockByProductIdQuery, ProductStockResponse> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var query = new GetStockByProductIdQuery(productId);

            var result = await handler.HandleAsync(query, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static (r) => TypedResults.Ok(r));
        });
        return app;
    }
}
