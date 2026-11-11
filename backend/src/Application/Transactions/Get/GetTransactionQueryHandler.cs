using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Transactions.Get;

internal sealed class GetTransactionQueryHandler(
    IApplicationDbContext dbContext,
    ILogger<GetTransactionQueryHandler> logger
) : IQueryHandler<GetTransactionQuery, PagedResponse<TransactionSummaryResponse>>
{
    private sealed class TransactionRow
    {
        public Guid Id { get; init; }
        public int Kind { get; init; }
        public string Title { get; init; } = string.Empty;
        public DateTime OccurrenceTime { get; init; }
        public int LineCount { get; init; }
        public long Quantity { get; init; }
        public decimal Total { get; init; }
    }

    public async Task<Result<PagedResponse<TransactionSummaryResponse>>> HandleAsync(GetTransactionQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(query.TimeZone);
            DateTime? startUtc = query.From.HasValue ? ToUtc(query.From.Value, timeZone) : null;
            DateTime? endUtc = query.To.HasValue ? ToUtc(query.To.Value.AddDays(1), timeZone) : null;
            TransactionKind? kind = TryParseKind(query.Kind, out var parsed) ? parsed : null;

            var sales = dbContext.Sales
                .Where(s => (startUtc == null || s.OccurenceTime >= startUtc)
                    && (endUtc == null || s.OccurenceTime < endUtc))
                .Select(s => new TransactionRow
                {
                    Id = s.Id,
                    Kind = (int)TransactionKind.Sale,
                    Title = s.Title,
                    OccurrenceTime = s.OccurenceTime,
                    LineCount = s.Products.Count,
                    Quantity = s.Products.Sum(e => (long)e.Quantity),
                    Total = s.Products.Sum(e => e.ProductPrice!.Value * e.Quantity),
                });

            var purchases = dbContext.Purchases
                .Where(p => (startUtc == null || p.OccurenceTime >= startUtc)
                    && (endUtc == null || p.OccurenceTime < endUtc))
                .Select(p => new TransactionRow
                {
                    Id = p.Id,
                    Kind = (int)TransactionKind.Purchase,
                    Title = p.Title,
                    OccurrenceTime = p.OccurenceTime,
                    LineCount = p.Products.Count,
                    Quantity = p.Products.Sum(e => (long)e.Quantity),
                    Total = p.Products.Sum(e => e.UnitCost * e.Quantity),
                });

            // Both sides have the same shape, so they combine in SQL and paging covers both at once.
            var rows = kind switch
            {
                TransactionKind.Sale => sales,
                TransactionKind.Purchase => purchases,
                _ => sales.Concat(purchases),
            };

            var totalCount = await rows.CountAsync(cancellationToken);

            var ordered = query.SortOrder == SortOrder.ASC
                ? rows.OrderBy(r => r.OccurrenceTime).ThenBy(r => r.Id)
                : rows.OrderByDescending(r => r.OccurrenceTime).ThenByDescending(r => r.Id);

            var page = await ordered
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            var responses = page
                .Select(r => new TransactionSummaryResponse(r.Id, (TransactionKind)r.Kind, r.Title, r.OccurrenceTime,
                    r.LineCount, r.Quantity, r.Total))
                .ToList();

            return PagedResponse<TransactionSummaryResponse>.Create(query.Page, query.PageSize, totalCount,
                responses);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(GetTransactionQueryHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while getting transactions with '{query}'", query);
            return ApplicationErrors.UnexpectedError(nameof(GetTransactionQueryHandler),
                $"Unexpected error has occurred while getting transactions with '{query}'");
        }
    }

    /// <summary>Reads a kind by name, ignoring case, and rejects numbers.</summary>
    public static bool TryParseKind(string? value, out TransactionKind kind)
    {
        var name = Enum.GetNames<TransactionKind>()
            .FirstOrDefault(n => string.Equals(n, value, StringComparison.OrdinalIgnoreCase));
        kind = name == null ? default : Enum.Parse<TransactionKind>(name);
        return name != null;
    }

    private static DateTime ToUtc(DateOnly localDate, TimeZoneInfo timeZone)
    {
        return TimeZoneInfo.ConvertTimeToUtc(localDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified),
            timeZone);
    }
}
