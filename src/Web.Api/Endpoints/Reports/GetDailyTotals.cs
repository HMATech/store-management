using Application.Abstractions.Messaging;
using Application.Reports.DailyTotals;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Reports;

internal sealed class GetDailyTotals : ReportEndpoint
{
    public sealed record GetDailyTotalsRequest(DateOnly? From, DateOnly? To, string? TimeZone);

    private const string DefaultTimeZone = "UTC";

    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/daily-totals", static async (
            [AsParameters] GetDailyTotalsRequest request,
            IQueryHandler<GetDailyTotalsQuery, DailyTotalsResponse> handler,
            CancellationToken cancellationToken
        ) =>
        {
            var query = new GetDailyTotalsQuery(request.From ?? default, request.To ?? default,
                string.IsNullOrWhiteSpace(request.TimeZone) ? DefaultTimeZone : request.TimeZone);

            var result = await handler.HandleAsync(query, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static r => TypedResults.Ok(r));
        });

        return app;
    }
}
