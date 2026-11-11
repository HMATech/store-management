using Application.Abstractions.Messaging;
using Application.Roles.Create;
using Microsoft.AspNetCore.Mvc;
using Web.Api.Infrastructure;
using Web.Api.Common;

namespace Web.Api.Endpoints.Roles;

internal sealed class Create : RoleEndpoint
{
    public sealed record CreateRoleRequest(string Name, string? Description);

    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/", static async (
            HttpContext httpContext,
            [FromBody] CreateRoleRequest request,
            ICommandHandler<CreateRoleCommand, CreateRoleResponse> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var command = new CreateRoleCommand(request.Name, request.Description);
            var result = await handler.HandleAsync(command, cancellationToken);

            return CustomHttpResults.TypedFrom(result,
                static (r, ctx) => TypedResults.Created($"{ctx.ToUriFullAbsolutePath()}/{r.Id}"),
                httpContext);
        });

        return app;
    }
}