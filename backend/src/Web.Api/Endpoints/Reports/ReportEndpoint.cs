namespace Web.Api.Endpoints.Reports;

public abstract class ReportEndpoint : IEndpoint
{
    IEndpointRouteBuilder IEndpoint.MapEndpoint(IEndpointRouteBuilder app)
    {
        MapEndpoint(app.MapGroup("/reports").WithTags(Tags.Reports));
        return app;
    }

    public abstract IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app);
}
