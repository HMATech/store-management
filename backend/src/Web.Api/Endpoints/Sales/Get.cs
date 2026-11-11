using Application.Abstractions.Messaging;
using Application.Common;
using Application.Sales;
using Application.Sales.Get;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Sales;

internal sealed class Get : SaleEndpoint
{
    public sealed record GetSaleRequest(
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
            [AsParameters] GetSaleRequest request,
            IQueryHandler<GetSaleQuery, PagedResponse<SaleResponse>> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var query = new GetSaleQuery(
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