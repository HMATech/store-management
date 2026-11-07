using Application.Abstractions.Messaging;
using Application.Stocks.StockAdjustments.DeleteById;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Stocks.StockAdjustments;

internal sealed class DeleteById : StockAdjustmentEndpoint
{
    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        base.MapEndpoint(app).MapDelete("/{id:guid}", static async (
            Guid id,
            ICommandHandler<DeleteStockAdjustmentByIdCommand> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var command = new DeleteStockAdjustmentByIdCommand(id);

            var result = await handler.HandleAsync(command, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static () => TypedResults.Ok());
        });
        return app;
    }
}
