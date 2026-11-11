using Application.Abstractions.Messaging;
using Application.Purchases;
using Application.Purchases.GetById;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Purchases;

internal sealed class GetById : PurchaseEndpoint
{
    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/{id:guid}", static async (
            Guid id,
            IQueryHandler<GetPurchaseByIdQuery, PurchaseResponse> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var query = new GetPurchaseByIdQuery(id);

            var result = await handler.HandleAsync(query, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static r => TypedResults.Ok(r));
        });

        return app;
    }
}