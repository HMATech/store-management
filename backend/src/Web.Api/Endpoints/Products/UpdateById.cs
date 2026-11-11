using Application.Abstractions.Messaging;
using Application.Products;
using Application.Products.UpdateById;
using Microsoft.AspNetCore.Mvc;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Products;

internal sealed class UpdateById : ProductEndpoint
{
    public sealed record StockDefinitionRequest(int? MinimumQuantity);

    public sealed record UpdateProductByIdRequest(string Name, string? Description,
        float Measure, int MeasureUnitId, decimal Price, string[] Tags,
        StockDefinitionRequest? StockDefinition = null);

    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/{id:guid}", static async (
            Guid id,
            [FromBody] UpdateProductByIdRequest request,
            ICommandHandler<UpdateProductByIdCommand, UpdateProductByIdResponse> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var command = new UpdateProductByIdCommand(id,
                request.Name, request.Description, request.Measure, request.MeasureUnitId,
                request.Price, request.Tags,
                request.StockDefinition == null
                    ? null
                    : new StockDefinitionCommand(request.StockDefinition.MinimumQuantity)
            );

            var result = await handler.HandleAsync(command, cancellationToken);

            return CustomHttpResults.TypedFrom(result,
                static (r) => TypedResults.Ok(r));
        });
        return app;
    }
}