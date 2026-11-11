using Application.Abstractions.Messaging;
using Application.Products.ProductTags.Create;
using Microsoft.AspNetCore.Mvc;
using Web.Api.Infrastructure;
using Web.Api.Common;

namespace Web.Api.Endpoints.Products.ProductTags;

internal sealed class Create : ProductTagEndpoint
{
    public sealed record CreateProductTagRequest(string Name);

    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        base.MapEndpoint(app).MapPost("/", static async (
            HttpContext httpContext,
            [FromBody] CreateProductTagRequest request,
            ICommandHandler<CreateProductTagCommand, CreateProductTagResponse> handler,
            ILogger<Create> logger,
            CancellationToken cancellationToken
        ) =>
        {
            var command = new CreateProductTagCommand(request.Name);

            var result = await handler.HandleAsync(command, cancellationToken);

            return CustomHttpResults.TypedFrom(result,
                static (r, ctx) => TypedResults.Created($"{ctx.ToUriFullAbsolutePath()}/{r.Id}", r),
                httpContext);
        });
        return app;
    }
}