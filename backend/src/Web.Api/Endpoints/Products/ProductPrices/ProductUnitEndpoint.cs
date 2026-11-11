namespace Web.Api.Endpoints.Products.ProductPrices;

public abstract class ProductPriceEndpoint : ProductEndpoint
{
    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        return app.MapGroup("/prices").WithTags(Tags.Products, Tags.ProductPrices);
    }
}