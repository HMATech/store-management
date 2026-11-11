namespace Web.Api.Endpoints.Products.ProductUnits;

public abstract class ProductUnitEndpoint : ProductEndpoint
{
    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        return app.MapGroup("/units").WithTags(Tags.Products, Tags.ProductUnits);
    }
}