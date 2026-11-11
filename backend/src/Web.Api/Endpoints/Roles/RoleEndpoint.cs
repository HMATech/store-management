namespace Web.Api.Endpoints.Roles;

public abstract class RoleEndpoint : IEndpoint
{
    IEndpointRouteBuilder IEndpoint.MapEndpoint(IEndpointRouteBuilder app)
    {
        MapEndpoint(app.MapGroup("/roles").WithTags(Tags.Roles));
        return app;
    }

    public abstract IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app);
}