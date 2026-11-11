using Web.Api.Infrastructure;
using Application.Users.Register;
using Application.Abstractions.Messaging;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Endpoints.Users;

internal sealed class Register : UserEndpoint
{
    public sealed record RegisterRequest(string Email, string Password, string FirstName, string? LastName);
    
    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/register", static async ([FromBody] RegisterRequest request,
            ICommandHandler<RegisterUserCommand, RegisterResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RegisterUserCommand(request.Email, request.Password,
                request.FirstName, request.LastName);

            var result = await handler.HandleAsync(command, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static r => TypedResults.Created());
        });

        return app;
    }
}