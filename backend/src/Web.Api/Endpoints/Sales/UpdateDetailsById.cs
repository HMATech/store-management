using Application.Abstractions.Messaging;
using Application.Sales.UpdateDetailsById;
using Microsoft.AspNetCore.Mvc;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Sales;

internal sealed class UpdateDetailsById : SaleEndpoint
{
    /// <param name="Title">Optional; leaving it out clears the title.</param>
    public sealed record UpdateSaleDetailRequest(
        string? Title,
        string[] Tags,
        DateTime OccurrenceTime
    );

    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/{id:guid}/details", static async (
            Guid id,
            [FromBody] UpdateSaleDetailRequest request,
            ICommandHandler<UpdateSaleDetailsByIdCommand, UpdateSaleDetailsByIdResponse> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var command = new UpdateSaleDetailsByIdCommand(
                id,
                request.Title ?? string.Empty,
                request.Tags,
                request.OccurrenceTime
            );

            var result = await handler.HandleAsync(command, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static r =>TypedResults.Ok(r));
        });

        return app;
    }
}