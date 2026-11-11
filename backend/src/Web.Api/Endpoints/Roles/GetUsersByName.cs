using Application.Abstractions.Messaging;
using Application.Roles.GetUsersByName;
using Application.Users;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Roles;

internal sealed class GetUsersByName : RoleEndpoint
{
    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/{role}/users", static async (string role,
            IQueryHandler<GetUsersByNameQuery, List<UserResponse>> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var query = new GetUsersByNameQuery(role);
            var result = await handler.HandleAsync(query, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static (r) => TypedResults.Ok(r));
        });

        return app;
    }
}