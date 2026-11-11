using Application.Abstractions.Messaging;
using Application.Products.ProductUnits.DeleteById;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Products.ProductUnits;

internal sealed class DeleteById : ProductUnitEndpoint
{
    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        base.MapEndpoint(app).MapDelete("/{id:int}", static async (
            int id,
            ICommandHandler<DeleteProductUnitByIdCommand> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var command = new DeleteProductUnitByIdCommand(id);

            var result = await handler.HandleAsync(command, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static () => TypedResults.Ok());
        });
        return app;
    }
}