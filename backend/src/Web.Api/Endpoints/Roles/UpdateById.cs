using Application.Abstractions.Messaging;
using Application.Roles.UpdateById;
using Microsoft.AspNetCore.Mvc;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Roles;

internal sealed class UpdateById : RoleEndpoint
{
    public sealed record UpdateRoleRequest(string Name, string? Description);

    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/{id:int}", static async (int id,
            [FromBody] UpdateRoleRequest request,
            ICommandHandler<UpdateRoleByIdCommand, UpdateRoleByIdResponse> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var command = new UpdateRoleByIdCommand(id, request.Name, request.Description);
            var result = await handler.HandleAsync(command, cancellationToken);

            return CustomHttpResults.TypedFrom(result,
                static (r) => TypedResults.Ok(r));
        });

        return app;
    }
}