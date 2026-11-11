using Application.Abstractions.Messaging;
using Application.Purchases.UpdateEntriesById;
using Microsoft.AspNetCore.Mvc;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Purchases;

internal sealed class UpdateEntriesById : PurchaseEndpoint
{
    public sealed record ProductEntry(Guid? Id, Guid ProductId, int Quantity, decimal UnitCost);

    public sealed record UpdatePurchaseRequest(
        ProductEntry[] ProductEntries
    );

    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/{id:guid}/entries", static async (
            Guid id,
            [FromBody] UpdatePurchaseRequest request,
            ICommandHandler<UpdatePurchaseEntriesByIdCommand, UpdatePurchaseEntriesByIdResponse> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var command = new UpdatePurchaseEntriesByIdCommand(
                id,
                request.ProductEntries?.Select(e => e == null ? null! : new ProductEntryCommand(
                    e.Id, e.ProductId, e.Quantity, e.UnitCost
                )).ToList()!
            );

            var result = await handler.HandleAsync(command, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static r =>TypedResults.Ok(r));
        });

        return app;
    }
}