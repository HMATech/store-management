using Application.Abstractions.Messaging;
using Application.Users.DeleteById;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class DeleteById : UserEndpoint
{
    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/{id}", static async (Guid id,
            ICommandHandler<DeleteUserByIdCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteUserByIdCommand(id);

            var result = await handler.HandleAsync(command, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static () => TypedResults.Ok());
        });

        return app;
    }
}