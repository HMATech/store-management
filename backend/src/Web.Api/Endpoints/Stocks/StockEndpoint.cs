
namespace Web.Api.Endpoints.Stocks;

public abstract class StockEndpoint : IEndpoint
{
    IEndpointRouteBuilder IEndpoint.MapEndpoint(IEndpointRouteBuilder app)
    {
        MapEndpoint(app.MapGroup("/stocks").WithTags(Tags.Stocks));
        return app;
    }

    public abstract IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app);
}
