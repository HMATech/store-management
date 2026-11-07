using Application.Abstractions.Messaging;
using Application.Common;
using Application.Users.Get;
using Application.Users;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class GetUser : UserEndpoint
{
    public sealed record GetUserRequest(string? Search, int Page = 1, int PageSize = 10);

    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/", static async ([AsParameters] GetUserRequest request,
            IQueryHandler<GetUserQuery, PagedResponse<UserResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetUserQuery(request.Search, request.Page < 1 ? 1 : request.Page, request.PageSize);

            var result = await handler.HandleAsync(query, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static r => TypedResults.Ok(r));
        });

        return app;
    }
}