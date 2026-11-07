using Application.Abstractions.Messaging;
using Application.Common;
using Application.Stocks.StockAdjustments;
using Application.Stocks.StockAdjustments.Get;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Stocks.StockAdjustments;

internal sealed class Get : StockAdjustmentEndpoint
{
    public sealed record GetStockAdjustmentRequest(Guid? ProductId, int Page = 1, int PageSize = 10);

    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        base.MapEndpoint(app).MapGet("/", static async (
            [AsParameters] GetStockAdjustmentRequest request,
            IQueryHandler<GetStockAdjustmentQuery, PagedResponse<StockAdjustmentResponse>> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var query = new GetStockAdjustmentQuery(request.ProductId, request.Page, request.PageSize);

            var result = await handler.HandleAsync(query, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static (r) => TypedResults.Ok(r));
        });
        return app;
    }
}
