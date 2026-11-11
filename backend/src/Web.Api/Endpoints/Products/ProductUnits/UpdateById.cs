using Application.Abstractions.Messaging;
using Application.Products.ProductUnits.UpdateById;
using Microsoft.AspNetCore.Mvc;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Products.ProductUnits;

internal sealed class UpdateById : ProductUnitEndpoint
{
    public sealed record UpdateProductUnitByIdRequest(string Name);

    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        base.MapEndpoint(app).MapPut("/{id:int}", static async (
            int id,
            [FromBody] UpdateProductUnitByIdRequest request,
            ICommandHandler<UpdateProductUnitByIdCommand, UpdateProductUnitByIdResponse> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var command = new UpdateProductUnitByIdCommand(id, request.Name);

            var result = await handler.HandleAsync(command, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static (r) => TypedResults.Ok(r));
        });
        return app;
    }
}