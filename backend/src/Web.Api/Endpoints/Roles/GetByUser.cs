using Application.Abstractions.Messaging;
using Application.Roles.GetRolesByUser;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Roles;

internal sealed class GetByUser : RoleEndpoint
{
    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/users/{user}", static async (string user,
            IQueryHandler<GetRolesByUserQuery, List<UserRoleResponse>> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var query = new GetRolesByUserQuery(user);
            var result = await handler.HandleAsync(query, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static (r) => TypedResults.Ok(r));
        });

        return app;
    }
}