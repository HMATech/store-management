using Application.Abstractions.Messaging;
using Application.Purchases.UpdateDetailsById;
using Microsoft.AspNetCore.Mvc;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Purchases;

internal sealed class UpdateDetailsById : PurchaseEndpoint
{
    public sealed record UpdatePurchaseDetailRequest(
        string Title,
        string[] Tags,
        DateTime OccurrenceTime
    );

    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/{id:guid}/details", static async (
            Guid id,
            [FromBody] UpdatePurchaseDetailRequest request,
            ICommandHandler<UpdatePurchaseDetailsByIdCommand, UpdatePurchaseDetailsByIdResponse> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var command = new UpdatePurchaseDetailsByIdCommand(
                id,
                request.Title,
                request.Tags,
                request.OccurrenceTime
            );

            var result = await handler.HandleAsync(command, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static r =>TypedResults.Ok(r));
        });

        return app;
    }
}