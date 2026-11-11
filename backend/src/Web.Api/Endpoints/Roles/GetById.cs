using Application.Abstractions.Messaging;
using Application.Roles;
using Application.Roles.GetById;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Roles;

internal sealed class GetById : RoleEndpoint
{
    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/{id:int}", static async (int id,
            IQueryHandler<GetRoleByIdQuery, RoleResponse> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var query = new GetRoleByIdQuery(id);
            var result = await handler.HandleAsync(query, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static (r) => TypedResults.Ok(r));
        });

        return app;
    }
}