using Application.Abstractions.Messaging;
using Application.Products.ProductTags.UpdateById;
using Microsoft.AspNetCore.Mvc;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Products.ProductTags;

internal sealed class UpdateById : ProductTagEndpoint
{
    public sealed record UpdateProductTagByIdRequest(string Name);

    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        base.MapEndpoint(app).MapPut("/{id:int}", static async (
            int id,
            [FromBody] UpdateProductTagByIdRequest request,
            ICommandHandler<UpdateProductTagByIdCommand, UpdateProductTagByIdResponse> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var command = new UpdateProductTagByIdCommand(id, request.Name);

            var result = await handler.HandleAsync(command, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static (r) => TypedResults.Ok(r));
        });
        return app;
    }
}