using Application.Abstractions.Messaging;
using Application.Products.ProductPrices.DeleteById;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Products.ProductPrices;

internal sealed class DeleteById : ProductPriceEndpoint
{
    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        base.MapEndpoint(app).MapDelete("/{id:guid}", static async (
            Guid id,
            ICommandHandler<DeleteProductPriceByIdCommand> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var command = new DeleteProductPriceByIdCommand(id);

            var result = await handler.HandleAsync(command, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static () => TypedResults.Ok());
        });
        return app;
    }
}