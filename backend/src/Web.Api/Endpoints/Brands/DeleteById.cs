using Application.Abstractions.Messaging;
using Application.Brands.DeleteById;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Brands;

internal sealed class DeleteById : BrandEndpoint
{
    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/{id:int}", static async (int id,
            ICommandHandler<DeleteBrandByIdCommand> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var command = new DeleteBrandByIdCommand(id);
            var result = await handler.HandleAsync(command, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static () => TypedResults.Ok());
        });

        return app;
    }
}