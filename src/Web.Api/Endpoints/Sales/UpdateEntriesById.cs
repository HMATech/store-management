using Application.Abstractions.Messaging;
using Application.Sales.UpdateEntriesById;
using Microsoft.AspNetCore.Mvc;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Sales;

internal sealed class UpdateEntriesById : SaleEndpoint
{
    public sealed record ProductEntry(Guid? Id, Guid ProductId, int Quantity);

    public sealed record UpdateSaleRequest(
        ProductEntry[] ProductEntries
    );

    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/{id:guid}/entries", static async (
            Guid id,
            [FromBody] UpdateSaleRequest request,
            ICommandHandler<UpdateSaleEntriesByIdCommand, UpdateSaleEntriesByIdResponse> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var command = new UpdateSaleEntriesByIdCommand(
                id,
                request.ProductEntries?.Select(e => e == null ? null! : new ProductEntryCommand(
                    e.Id, e.ProductId, e.Quantity
                )).ToList()!
            );

            var result = await handler.HandleAsync(command, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static r =>TypedResults.Ok(r));
        });

        return app;
    }
}