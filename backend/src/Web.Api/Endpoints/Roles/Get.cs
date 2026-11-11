using Application.Abstractions.Messaging;
using Application.Common;
using Application.Roles;
using Application.Roles.Get;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Roles;

internal sealed class Get : RoleEndpoint
{
    public sealed record GetRolesRequest(string? Search,
        string? SortOrder);

    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/", static async ([AsParameters] GetRolesRequest request,
            IQueryHandler<GetRoleQuery, List<RoleResponse>> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var query = new GetRoleQuery(request.Search,
                Enum.TryParse<SortOrder>(request.SortOrder, out var order)
                ? order : SortOrder.ASC);
            var result = await handler.HandleAsync(query, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static (r) => TypedResults.Ok(r));
        });

        return app;
    }
}