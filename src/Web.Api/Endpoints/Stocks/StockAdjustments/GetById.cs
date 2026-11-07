using Application.Abstractions.Messaging;
using Application.Stocks.StockAdjustments;
using Application.Stocks.StockAdjustments.GetById;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Stocks.StockAdjustments;

internal sealed class GetById : StockAdjustmentEndpoint
{
    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        base.MapEndpoint(app).MapGet("/{id:guid}", static async (
            Guid id,
            IQueryHandler<GetStockAdjustmentByIdQuery, StockAdjustmentResponse> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var query = new GetStockAdjustmentByIdQuery(id);

            var result = await handler.HandleAsync(query, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static (r) => TypedResults.Ok(r));
        });
        return app;
    }
}
