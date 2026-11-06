using Application.Abstractions.Messaging;
using Application.Sales.Create;
using Web.Api.Common;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Sales;

internal sealed class Create : SaleEndpoint
{
    public sealed record ProductEntry(Guid Id, int Quantity);

    public sealed record CreateSaleRequest(
        string Title,
        string[] Tags,
        ProductEntry[] ProductEntries,
        DateTime OccurrenceTime
    );

    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/", static async (
            HttpContext httpContext,
            CreateSaleRequest request,
            ICommandHandler<CreateSaleCommand, CreateSaleResponse> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var command = new CreateSaleCommand(
                request.Title,
                request.Tags,
                request.ProductEntries?.Select(e => e == null ? null! : new ProductEntryCommand(
                    e.Id, e.Quantity
                )).ToList()!,
                request.OccurrenceTime
            );

            var result = await handler.HandleAsync(command, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static (r, ctx) =>
                TypedResults.Created($"{ctx.ToUriFullAbsolutePath()}/{r.Id}"), httpContext);
        });

        return app;
    }
}