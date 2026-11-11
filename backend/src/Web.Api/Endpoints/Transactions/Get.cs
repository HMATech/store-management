using Application.Abstractions.Messaging;
using Application.Common;
using Application.Transactions;
using Application.Transactions.Get;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Transactions;

internal sealed class Get : TransactionEndpoint
{
    public sealed record GetTransactionRequest(string? SortOrder, string? Kind, DateOnly? From, DateOnly? To,
        string? TimeZone, int Page = 1, int PageSize = 10);

    private const string DefaultTimeZone = "UTC";

    public override IEndpointRouteBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/", static async (
            [AsParameters] GetTransactionRequest request,
            IQueryHandler<GetTransactionQuery, PagedResponse<TransactionSummaryResponse>> handler,
            CancellationToken cancellationToken
        ) =>
        {
            // Newest first unless asked otherwise, as a history is usually read.
            var query = new GetTransactionQuery(
                Enum.TryParse<SortOrder>(request.SortOrder, out var order) ? order : SortOrder.DESC,
                request.Kind, request.From, request.To,
                string.IsNullOrWhiteSpace(request.TimeZone) ? DefaultTimeZone : request.TimeZone,
                request.Page, request.PageSize);

            var result = await handler.HandleAsync(query, cancellationToken);

            return CustomHttpResults.TypedFrom(result, static r => TypedResults.Ok(r));
        });

        return app;
    }
}
