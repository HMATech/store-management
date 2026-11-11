using Application.Abstractions.Messaging;
using Application.Sales.DeleteById;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Sales;

internal sealed class DeleteById : SaleEndpoint
{
    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/{id:guid}", static async (
            Guid id,
            ICommandHandler<DeleteSaleByIdCommand> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var command = new DeleteSaleByIdCommand(id);

            var result = await handler.HandleAsync(command, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static () => TypedResults.Ok());
        });

        return app;
    }
}