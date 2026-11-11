
namespace Web.Api.Endpoints.Users;

public abstract class UserEndpoint : IEndpoint
{
    IEndpointRouteBuilder IEndpoint.MapEndpoint(IEndpointRouteBuilder app)
    {
        MapEndpoint(app.MapGroup("/users").WithTags(Tags.Users));
        return app;
    }

    public abstract IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app);
}