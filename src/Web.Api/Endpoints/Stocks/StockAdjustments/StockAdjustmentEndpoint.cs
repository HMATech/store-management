namespace Web.Api.Endpoints.Stocks.StockAdjustments;

public abstract class StockAdjustmentEndpoint : StockEndpoint
{
    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        return app.MapGroup("/adjustments").WithTags(Tags.Stocks, Tags.StockAdjustments);
    }
}
