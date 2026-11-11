using Application.Abstractions.Messaging;
using Application.Common;
using Application.Stocks;
using Application.Stocks.Get;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Stocks;

internal sealed class Get : StockEndpoint
{
    public sealed record GetStockRequest(string? Search, string? SortOrder, int Page = 1, int PageSize = 10,
        string? Status = null);

    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/", static async (
            [AsParameters] GetStockRequest request,
            IQueryHandler<GetStockQuery, PagedResponse<ProductStockResponse>> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var query = new GetStockQuery(request.Search,
                Enum.TryParse<SortOrder>(request.SortOrder, out var order)
                ? order : SortOrder.ASC, request.Page, request.PageSize, request.Status);

            var result = await handler.HandleAsync(query, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static (r) => TypedResults.Ok(r));
        });
        return app;
    }
}
