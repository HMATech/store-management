using Application.Abstractions.Messaging;
using Application.Products.ProductTags.DeleteById;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Products.ProductTags;

internal sealed class DeleteById : ProductTagEndpoint
{
    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        base.MapEndpoint(app).MapDelete("/{id:int}", static async (
            int id,
            ICommandHandler<DeleteProductTagByIdCommand> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var command = new DeleteProductTagByIdCommand(id);

            var result = await handler.HandleAsync(command, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static () => TypedResults.Ok());
        });
        return app;
    }
}