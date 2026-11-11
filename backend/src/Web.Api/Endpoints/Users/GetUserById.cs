using Application.Abstractions.Messaging;
using Application.Users;
using Application.Users.GetById;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class GetUserById : UserEndpoint
{
    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/{id}", static async (Guid id,
            IQueryHandler<GetUserByIdQuery, UserResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetUserByIdQuery(id);
            var result = await handler.HandleAsync(query, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static r => TypedResults.Ok(r));
        });

        return app;
    }
}