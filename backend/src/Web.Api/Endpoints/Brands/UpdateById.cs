using Application.Abstractions.Messaging;
using Application.Brands.UpdateById;
using Microsoft.AspNetCore.Mvc;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Brands;

internal sealed class UpdateById : BrandEndpoint
{
    public sealed record UpdateBrandRequest(string Name);

    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/{id:int}", static async (int id,
            [FromBody] UpdateBrandRequest request,
            ICommandHandler<UpdateBrandByIdCommand, UpdateBrandByIdResponse> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var command = new UpdateBrandByIdCommand(id, request.Name);
            var result = await handler.HandleAsync(command, cancellationToken);

            return CustomHttpResults.TypedFrom(result,
                static (r) => TypedResults.Ok(r));
        });

        return app;
    }
}