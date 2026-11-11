using Application.Abstractions.Messaging;
using Application.Purchases.DeleteById;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Purchases;

internal sealed class DeleteById : PurchaseEndpoint
{
    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/{id:guid}", static async (
            Guid id,
            ICommandHandler<DeletePurchaseByIdCommand> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var command = new DeletePurchaseByIdCommand(id);

            var result = await handler.HandleAsync(command, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static () => TypedResults.Ok());
        });

        return app;
    }
}