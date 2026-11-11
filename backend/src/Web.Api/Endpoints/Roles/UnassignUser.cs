using Application.Abstractions.Messaging;
using Application.Roles.UnassignUser;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Roles;

internal sealed class UnssignUser : RoleEndpoint
{
    public sealed record AssignUserRequest(int RoleId, string User);

    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/{roleId}/users/{user}", static async (
            int roleId,
            string user,
            ICommandHandler<UnassignUserCommand> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var command = new UnassignUserCommand(roleId, user);
            var result = await handler.HandleAsync(command, cancellationToken);

            return CustomHttpResults.TypedFrom(result,
                static () => TypedResults.Ok());
        });

        return app;
    }
}