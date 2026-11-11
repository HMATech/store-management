using Application.Abstractions.Messaging;
using Application.Common;
using Application.Purchases;
using Application.Purchases.Get;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Purchases;

internal sealed class Get : PurchaseEndpoint
{
    public sealed record GetPurchaseRequest(
        string? Search,
        string? SortOrder,
        DateOnly? OccurenceFrom,
        DateOnly? OccurenceTo,
        decimal? TotalFrom,
        decimal? TotalTo,
        int Page = 1,
        int PageSize = 10
    );

    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/", static async (
            [AsParameters] GetPurchaseRequest request,
            IQueryHandler<GetPurchaseQuery, PagedResponse<PurchaseResponse>> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var query = new GetPurchaseQuery(
                request.Search,
                Enum.TryParse<SortOrder>(request.SortOrder, out var order)
                ? order : SortOrder.ASC,
                request.OccurenceFrom,
                request.OccurenceTo,
                request.TotalFrom,
                request.TotalTo,
                request.Page,
                request.PageSize
            );

            var result = await handler.HandleAsync(query, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static r => TypedResults.Ok(r));
        });

        return app;
    }
}