namespace Web.Api.Endpoints;

public interface IEndpoint
{
    IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app);
}