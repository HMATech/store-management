namespace Web.Api.Endpoints.Products.ProductTags;

public abstract class ProductTagEndpoint : ProductEndpoint
{
    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        return app.MapGroup("/tags").WithTags(Tags.Products, Tags.ProductTags);
    }
}