namespace Web.Api.Endpoints.Purchases;

public abstract class PurchaseEndpoint : IEndpoint
{
    IEndpointRouteBuilder IEndpoint.MapEndpoint(IEndpointRouteBuilder app)
    {
        MapEndpoint(app.MapGroup("/purchases").WithTags(Tags.Purchases));
        return app;
    }

    public abstract IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app);
}