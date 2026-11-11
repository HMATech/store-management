using Application.Abstractions.Messaging;
using Application.Products.ProductPrices.UpdateById;
using Microsoft.AspNetCore.Mvc;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Products.ProductPrices;

internal sealed class UpdateById : ProductPriceEndpoint
{
    public sealed record UpdateProductPriceByIdRequest(decimal Value);

    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        base.MapEndpoint(app).MapPut("/{id:guid}", static async (
            Guid id,
            [FromBody] UpdateProductPriceByIdRequest request,
            ICommandHandler<UpdateProductPriceByIdCommand, UpdateProductPriceByIdResponse> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var command = new UpdateProductPriceByIdCommand(id, request.Value);

            var result = await handler.HandleAsync(command, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static (r) => TypedResults.Ok(r));
        });
        return app;
    }
}