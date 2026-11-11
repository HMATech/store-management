using Application.Abstractions.Messaging;
using Application.Roles.DeleteById;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Roles;

internal sealed class DeleteById : RoleEndpoint
{
    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/{id:int}", static async (int id,
            ICommandHandler<DeleteRoleByIdCommand> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var command = new DeleteRoleByIdCommand(id);
            var result = await handler.HandleAsync(command, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static () => TypedResults.Ok());
        });

        return app;
    }
}