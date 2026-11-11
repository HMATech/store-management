using Application.Abstractions.Messaging;
using Application.Brands;
using Application.Brands.GetById;
using Application.Roles;
using Application.Roles.GetById;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Brands;

internal sealed class GetById : BrandEndpoint
{
    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/{id:int}", static async (int id,
            IQueryHandler<GetBrandByIdQuery, BrandResponse> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var query = new GetBrandByIdQuery(id);
            var result = await handler.HandleAsync(query, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static (r) => TypedResults.Ok(r));
        });

        return app;
    }
}