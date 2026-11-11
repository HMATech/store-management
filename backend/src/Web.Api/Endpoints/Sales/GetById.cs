using Application.Abstractions.Messaging;
using Application.Sales;
using Application.Sales.GetById;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Sales;

internal sealed class GetById : SaleEndpoint
{
    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/{id:guid}", static async (
            Guid id,
            IQueryHandler<GetSaleByIdQuery, SaleResponse> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var query = new GetSaleByIdQuery(id);

            var result = await handler.HandleAsync(query, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static r => TypedResults.Ok(r));
        });

        return app;
    }
}