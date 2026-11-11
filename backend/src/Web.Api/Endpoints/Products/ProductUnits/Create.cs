using Application.Abstractions.Messaging;
using Application.Products.ProductUnits.Create;
using Microsoft.AspNetCore.Mvc;
using Web.Api.Infrastructure;
using Web.Api.Common;

namespace Web.Api.Endpoints.Products.ProductUnits;

internal sealed class Create : ProductUnitEndpoint
{
    public sealed record CreateProductUnitRequest(string Name);

    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        base.MapEndpoint(app).MapPost("/", static async (
            HttpContext httpContext,
            [FromBody] CreateProductUnitRequest request,
            ICommandHandler<CreateProductUnitCommand, CreateProductUnitResponse> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var command = new CreateProductUnitCommand(request.Name);

            var result = await handler.HandleAsync(command, cancellationToken);

            return CustomHttpResults.TypedFrom(result,
                static (r, ctx) => TypedResults.Created($"{ctx.ToUriFullAbsolutePath()}/{r.Id}", r),
                httpContext);
        });
        return app;
    }
}