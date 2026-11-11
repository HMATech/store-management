using Application.Abstractions.Messaging;
using Application.Brands.Create;
using Microsoft.AspNetCore.Mvc;
using Web.Api.Infrastructure;
using Web.Api.Common;

namespace Web.Api.Endpoints.Brands;

internal sealed class Create : BrandEndpoint
{
    public sealed record CreateBrandRequest(string Name);

    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/", static async (
            HttpContext httpContext,
            [FromBody] CreateBrandRequest request,
            ICommandHandler<CreateBrandCommand, CreateBrandResponse> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var command = new CreateBrandCommand(request.Name);
            var result = await handler.HandleAsync(command, cancellationToken);

            return CustomHttpResults.TypedFrom(result,
                static (r, ctx) => TypedResults.Created($"{ctx.ToUriFullAbsolutePath()}/{r.Id}"),
                httpContext);
        });

        return app;
    }
}