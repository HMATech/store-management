using Application.Abstractions.Messaging;
using Application.Roles.AssignUser;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Roles;

internal sealed class AssignUser : RoleEndpoint
{
    public sealed record AssignUserRequest(int RoleId, string User);

    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/{roleId}/users/{user}", static async (
            int roleId,
            string user,
            ICommandHandler<AssignUserCommand, AssignUserResponse> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var command = new AssignUserCommand(roleId, user);
            var result = await handler.HandleAsync(command, cancellationToken);

            return CustomHttpResults.TypedFrom(result,
                static () => TypedResults.Created());
        });

        return app;
    }
}