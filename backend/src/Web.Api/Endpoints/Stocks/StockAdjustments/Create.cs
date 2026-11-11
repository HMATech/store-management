using Application.Abstractions.Messaging;
using Application.Stocks.StockAdjustments.Create;
using Microsoft.AspNetCore.Mvc;
using Web.Api.Common;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Stocks.StockAdjustments;

internal sealed class Create : StockAdjustmentEndpoint
{
    public sealed record CreateStockAdjustmentRequest(Guid ProductId, int Quantity, string Reason,
        DateTime OccurrenceTime);

    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        base.MapEndpoint(app).MapPost("/", static async (
            HttpContext httpContext,
            [FromBody] CreateStockAdjustmentRequest request,
            ICommandHandler<CreateStockAdjustmentCommand, CreateStockAdjustmentResponse> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var command = new CreateStockAdjustmentCommand(request.ProductId, request.Quantity, request.Reason,
                request.OccurrenceTime);

            var result = await handler.HandleAsync(command, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static (r, ctx) =>
                TypedResults.Created($"{ctx.ToUriFullAbsolutePath()}/{r.Id}"), httpContext);
        });
        return app;
    }
}
