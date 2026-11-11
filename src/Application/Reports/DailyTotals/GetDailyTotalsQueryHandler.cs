using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Reports.DailyTotals;

internal sealed class GetDailyTotalsQueryHandler(
    IApplicationDbContext dbContext,
    ILogger<GetDailyTotalsQueryHandler> logger
) : IQueryHandler<GetDailyTotalsQuery, DailyTotalsResponse>
{
    private sealed record TransactionTotal(DateTime OccurrenceTime, decimal Total);

    public async Task<Result<DailyTotalsResponse>> HandleAsync(GetDailyTotalsQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(query.TimeZone);
            var startUtc = ToUtc(query.From, timeZone);
            var endUtc = ToUtc(query.To.AddDays(1), timeZone);

            // Each transaction's total is summed in SQL; only the per-day grouping happens here,
            // because the day depends on the time zone.
            var sales = await dbContext.Sales
                .Where(s => s.OccurenceTime >= startUtc && s.OccurenceTime < endUtc)
                .Select(s => new TransactionTotal(s.OccurenceTime,
                    s.Products.Sum(e => e.ProductPrice!.Value * e.Quantity)))
                .ToListAsync(cancellationToken);

            var purchases = await dbContext.Purchases
                .Where(p => p.OccurenceTime >= startUtc && p.OccurenceTime < endUtc)
                .Select(p => new TransactionTotal(p.OccurenceTime,
                    p.Products.Sum(e => e.UnitCost * e.Quantity)))
                .ToListAsync(cancellationToken);

            var salesByDay = GroupByLocalDay(sales, timeZone);
            var purchasesByDay = GroupByLocalDay(purchases, timeZone);

            var days = Enumerable.Range(0, query.To.DayNumber - query.From.DayNumber + 1)
                .Select(offset => query.From.AddDays(offset))
                .Select(date =>
                {
                    var (salesTotal, salesCount) = salesByDay.GetValueOrDefault(date);
                    var (purchasesTotal, purchasesCount) = purchasesByDay.GetValueOrDefault(date);
                    return new DayTotalsResponse(date, salesTotal, salesCount, purchasesTotal, purchasesCount);
                })
                .ToArray();

            return new DailyTotalsResponse(query.From, query.To, query.TimeZone,
                days.Sum(d => d.SalesTotal), days.Sum(d => d.SalesCount),
                days.Sum(d => d.PurchasesTotal), days.Sum(d => d.PurchasesCount),
                days);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(GetDailyTotalsQueryHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while getting daily totals with '{query}'", query);
            return ApplicationErrors.UnexpectedError(nameof(GetDailyTotalsQueryHandler),
                $"Unexpected error has occurred while getting daily totals with '{query}'");
        }
    }

    private static DateTime ToUtc(DateOnly localDate, TimeZoneInfo timeZone)
    {
        return TimeZoneInfo.ConvertTimeToUtc(localDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified),
            timeZone);
    }

    private static Dictionary<DateOnly, (decimal Total, int Count)> GroupByLocalDay(
        IEnumerable<TransactionTotal> transactions, TimeZoneInfo timeZone)
    {
        return transactions
            .GroupBy(t => DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(t.OccurrenceTime, DateTimeKind.Utc), timeZone)))
            .ToDictionary(g => g.Key, g => (g.Sum(t => t.Total), g.Count()));
    }
}
