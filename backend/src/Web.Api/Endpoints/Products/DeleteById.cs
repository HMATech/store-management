using Application.Abstractions.Messaging;
using Application.Products.DeleteById;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Products;

internal sealed class DeleteById : ProductEndpoint
{
    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/{id:guid}", static async (
            Guid id,
            ICommandHandler<DeleteProductByIdCommand> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var command = new DeleteProductByIdCommand(id);

            var result = await handler.HandleAsync(command, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static () => TypedResults.Ok());
        });
        return app;
    }
}